public interface IHealth
{
    float GetCurrentHealth();
    float GetMaxHealth();

    event System.Action<float, float> OnHealthChanged;
}