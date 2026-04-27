using UnityEngine;
using System.Collections.Generic;

public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [SerializeField] private AudioSource sfxSourcePrefab;
    [SerializeField] private int poolSize = 10;
    
    private Queue<AudioSource> _pool = new();
    private List<AudioSource> _activeSources = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize pool
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = Instantiate(sfxSourcePrefab, transform);
            source.gameObject.SetActive(false);
            _pool.Enqueue(source);
        }
    }

    public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetSource();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();

        _activeSources.Add(source);
    }

    public void PlaySFXAtPoint(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetSource();
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();

        _activeSources.Add(source);
    }

    private void Update()
    {
        // Return finished sources to pool
        for (int i = _activeSources.Count - 1; i >= 0; i--)
        {
            if (!_activeSources[i].isPlaying)
            {
                ReturnToPool(_activeSources[i]);
                _activeSources.RemoveAt(i);
            }
        }
    }

    private AudioSource GetSource()
    {
        if (_pool.Count > 0)
        {
            AudioSource source = _pool.Dequeue();
            source.gameObject.SetActive(true);
            return source;
        }

        // Pool exhausted, create new source
        AudioSource newSource = Instantiate(sfxSourcePrefab, transform);
        return newSource;
    }

    private void ReturnToPool(AudioSource source)
    {
        source.Stop();
        source.gameObject.SetActive(false);
        _pool.Enqueue(source);
    }
}
