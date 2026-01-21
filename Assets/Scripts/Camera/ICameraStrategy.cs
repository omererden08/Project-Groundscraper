public interface ICameraStrategy
{
    void OnEnter(CameraController controller);
    void OnExit();
    void TickLate(float dt);
}
