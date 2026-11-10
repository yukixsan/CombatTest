using System.Collections.Generic;
using System.Text;
using Unity.Profiling;
using UnityEngine;

/// <summary>
/// Lightweight in-game profiler summary. Add to any GameObject in Play mode; after
/// <see cref="sampleFrames"/> frames it logs average / worst values of the key frame,
/// render and script counters, then (optionally) destroys itself.
/// Works in Editor and Development builds (release builds expose fewer counters).
/// </summary>
public class CombatPerfProbe : MonoBehaviour
{
    [Min(30)] public int sampleFrames = 300;
    [Tooltip("Frames to skip first (loading hitches, shader warm-up).")]
    [Min(0)] public int warmupFrames = 60;
    public bool destroyWhenDone = true;

    private struct Counter
    {
        public string label;
        public ProfilerRecorder recorder;
        public double scale;     // to ms / count
        public string unit;
        public double sum, max;
        public int n;
    }

    private readonly List<Counter> _counters = new List<Counter>();
    private int _frame;
    private readonly FrameTiming[] _timings = new FrameTiming[1];
    private double _gpuSum, _gpuMax; private int _gpuN;

    public static string LastReport { get; private set; }

    private void OnEnable()
    {
        const double ns = 1e-6;
        Add("CPU main thread",   ProfilerCategory.Internal, "CPU Main Thread Frame Time",   ns, "ms");
        Add("CPU render thread", ProfilerCategory.Internal, "CPU Render Thread Frame Time", ns, "ms");
        Add("PlayerLoop",        ProfilerCategory.Internal, "PlayerLoop",                   ns, "ms");
        Add("Scripts Update",    ProfilerCategory.Scripts,  "BehaviourUpdate",              ns, "ms");
        Add("Scripts FixedUpd",  ProfilerCategory.Scripts,  "FixedBehaviourUpdate",         ns, "ms");
        Add("Scripts LateUpd",   ProfilerCategory.Scripts,  "LateBehaviourUpdate",          ns, "ms");
        Add("Physics",           ProfilerCategory.Physics,  "Physics.Simulate",             ns, "ms");
        Add("Animators",         ProfilerCategory.Animation,"Animators.Update",             ns, "ms");
        Add("Render (camera)",   ProfilerCategory.Render,   "UniversalRenderPipeline.RenderSingleCameraInternal", ns, "ms");
        Add("Shadows",           ProfilerCategory.Render,   "MainLightShadowCasterPass",    ns, "ms");
        Add("Editor overhead",   ProfilerCategory.Internal, "EditorLoop",                   ns, "ms");
        Add("GC alloc/frame",    ProfilerCategory.Memory,   "GC Allocated In Frame",        1.0 / 1024, "KB");
        Add("Batches",           ProfilerCategory.Render,   "Batches Count",                1, "");
        Add("SetPass calls",     ProfilerCategory.Render,   "SetPass Calls Count",          1, "");
        Add("Draw calls",        ProfilerCategory.Render,   "Draw Calls Count",             1, "");
        Add("Triangles",         ProfilerCategory.Render,   "Triangles Count",              1, "");
        Add("Shadow casters",    ProfilerCategory.Render,   "Shadow Casters Count",         1, "");
        Add("Skinned meshes",    ProfilerCategory.Render,   "Visible Skinned Meshes Count", 1, "");
        _frame = 0;
    }

    private void Add(string label, ProfilerCategory cat, string stat, double scale, string unit)
    {
        var r = ProfilerRecorder.StartNew(cat, stat);
        _counters.Add(new Counter { label = label, recorder = r, scale = scale, unit = unit });
    }

    private void OnDisable()
    {
        foreach (var c in _counters) c.recorder.Dispose();
        _counters.Clear();
    }

    private void Update()
    {
        _frame++;
        if (_frame <= warmupFrames) return;

        for (int i = 0; i < _counters.Count; i++)
        {
            var c = _counters[i];
            if (!c.recorder.Valid) continue;
            double v = c.recorder.LastValue * c.scale;
            c.sum += v; c.n++; if (v > c.max) c.max = v;
            _counters[i] = c;
        }

        FrameTimingManager.CaptureFrameTimings();
        if (FrameTimingManager.GetLatestTimings(1, _timings) > 0 && _timings[0].gpuFrameTime > 0)
        {
            double g = _timings[0].gpuFrameTime;
            _gpuSum += g; _gpuN++; if (g > _gpuMax) _gpuMax = g;
        }

        if (_frame >= warmupFrames + sampleFrames) Report();
    }

    private void Report()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[CombatPerfProbe] {sampleFrames} frames  (avg / worst)   fps≈{1f / Time.smoothDeltaTime:F0}  " +
                      $"vSync={QualitySettings.vSyncCount} targetFps={Application.targetFrameRate} " +
                      $"fixedDt={Time.fixedDeltaTime:F4} renderScale={(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)?.renderScale:F2} " +
                      $"res={Screen.width}x{Screen.height}");
        if (_gpuN > 0) sb.AppendLine($"  GPU frame          {_gpuSum / _gpuN,8:F2} / {_gpuMax,8:F2} ms");
        else sb.AppendLine("  GPU frame          n/a (enable Frame Timing Stats in Player Settings)");
        foreach (var c in _counters)
        {
            if (c.n == 0) { sb.AppendLine($"  {c.label,-18} n/a"); continue; }
            sb.AppendLine($"  {c.label,-18} {c.sum / c.n,8:F2} / {c.max,8:F2} {c.unit}");
        }
        LastReport = sb.ToString();
        Debug.Log(LastReport);
        if (destroyWhenDone) Destroy(this);
        else { _frame = 0; for (int i = 0; i < _counters.Count; i++) { var c = _counters[i]; c.sum = c.max = 0; c.n = 0; _counters[i] = c; } _gpuSum = _gpuMax = 0; _gpuN = 0; }
    }
}
