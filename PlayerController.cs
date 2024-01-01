using Godot;

public class PlayerController
{
    private readonly Player player;
    private Vector3 velocity = new();

    private GodotObject colidingObject;

    public PlayerController(Player player)
    {
        this.player = player;
    }


    /// <summary>
    /// Rotates Player model & camera
    /// </summary>
    /// <param name="mouseDelta">new rotation</param>
    /// <param name="camera">Player camera</param>
    public void RotateByMouseDelta(Vector2 mouseDelta, Camera3D camera)
    {
        // Rotate the player around the Y-axis (left and right)
        player.RotateY(-mouseDelta.X * player.RotationSpeed);

        // Rotate the camera around the X-axis (up and down)
        camera.RotateX(-mouseDelta.Y * player.RotationSpeed);

        // Clamp the camera's rotation so you can't look too far up or down
        float cameraRotation = Mathf.Clamp(camera.RotationDegrees.X, -70, 70);
        camera.RotationDegrees = new Vector3(cameraRotation, 0, 0);
    }

    public void UpdateLookAtObject(RayCast3D rayCast)
    {
        var newColidingObject = rayCast.GetCollider();
        colidingObject = newColidingObject;

        var isValidObject = colidingObject is not null;
        if (isValidObject)
        {
            player.EmitSignal(nameof(player.OnRaycastColide), newColidingObject);
        }
    }
}
