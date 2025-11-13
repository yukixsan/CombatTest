using System.Collections.Generic;
using UnityEngine;

public class CommandBuffer : MonoBehaviour
{
    private readonly Queue<BufferedCommand> _queue = new();
    private const float BUFFER_DURATION = 0.25f; // static for now
    [SerializeField] private int maxBufferSize = 1;  // max queued commands to keep (FIFO)
    [SerializeField] private float minEnqueueInterval = 0.06f; // debounce per command

    private float _lastEnqueueTime = -999f;
    private CommandType _lastEnqueuedType = CommandType.Attack;
    private int _lastEnqueuedSkill = -1;

    public void Enqueue(CommandType type, int skillIndex = -1)
    {
        // Debounce: if the exact same command was enqueued very recently, ignore duplicate spamming
        if (Time.time - _lastEnqueueTime < minEnqueueInterval &&
            type == _lastEnqueuedType && skillIndex == _lastEnqueuedSkill)
        {
            return;
        }

        _lastEnqueueTime = Time.time;
        _lastEnqueuedType = type;
        _lastEnqueuedSkill = skillIndex;

        // If buffer is full, drop the oldest (FIFO) to make room
        while (_queue.Count >= maxBufferSize)
            _queue.Dequeue();

        _queue.Enqueue(new BufferedCommand(type, Time.time, skillIndex));
    }

    public void Process(CommandInterpreter interpreter, bool allowed)
    {
        while (_queue.Count > 0 && Time.time - _queue.Peek().time > BUFFER_DURATION)
        {
            _queue.Dequeue();
        }
        if (!allowed)
            return;
        if(_queue.Count > 0)
        {
            var cmd = _queue.Dequeue();
            interpreter.Receive(cmd);
        }
    }

    public void Clear() => _queue.Clear();
}

public struct BufferedCommand
{
    public CommandType type;
    public float time;
    public int skillIndex;

    public BufferedCommand(CommandType t, float tm, int skill = -1)
    {
        type = t;
        time = tm;
        skillIndex = skill;
    }
}
