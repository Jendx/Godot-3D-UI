using Godot;

/// <summary>
/// Base class for Nodes that can be controlled by user's raycast (Basicaly Raycast replaces mouse)
/// For correct use create Node structure as follows:
/// <code>
/// L RaycastInteractable2DUiNode3D
/// LL SubViewport
/// LLL Camera (Render viewport texture into SubViewport)
/// LL MeshInstance3D (Create material with ViewPortTexture)
/// LLL Area3D (For tracking raycast. <b>DO NOT FORGET TO SET RAYCAST TO COLIDE WITH AREAS</b>)
/// LLLL ColisionShape3D
/// </code>
/// </summary>
[GlobalClass]
public abstract partial class RaycastInteractable2DUiNode3D : Node3D
{
    private bool isMouseHeld;
    private bool isMouseInside;
    private Vector3? lastMousePosition3D;
    private Vector2 quadSize;
    private Vector2 lastMousePosition2D = Vector2.Zero;

    [Export]
    protected Area3D uiArea;

    [Export]
    protected SubViewport subViewport;

    [Export]
    protected MeshInstance3D cameraViewDisplayMesh;

    protected RaycastInteractable2DUiNode3D() { }

    /// <summary>
    /// If event is mouse event & mouse is in area, the input will be forwarded into subViewport
    /// </summary>
    /// <param name="event"></param>
    public override void _UnhandledInput(InputEvent @event)
    {
        bool isMouseEvent = @event is InputEventMouse or InputEventMouseMotion or InputEventMouseButton;

        if (isMouseEvent && (isMouseInside || isMouseHeld))
        {
            HandleMouse((InputEventMouse)@event);
        }

        if (!isMouseEvent)
        {
            subViewport.PushInput(@event);
            return;
        }
    }

    /// <summary>
    /// Get's called everytime player's raycast hits valid object (Signal receiving method)
    /// </summary>
    /// <param name="colidedObject"></param>
    public void Player_onRayCastColided(Node colidedObject)
    {
        if (colidedObject.Name == uiArea.Name)
        {
            isMouseInside = true;
        }
    }

    private void HandleMouse(InputEventMouse @event)
    {
        isMouseInside = FindMouse(@event.GlobalPosition, out Vector3 position);

        HandleMouseInPosition(@event, position);
    }

    public void HandleSynteticMouseMotion(Vector3 position)
    {
        isMouseInside = true;

        HandleMouseInPosition(new InputEventMouseMotion(), position);
    }

    public void HandleSynteticMouseClick(Vector3 position, bool pressed)
    {
        isMouseInside = true;

        HandleMouseInPosition(
            new InputEventMouseButton() { ButtonIndex = MouseButton.Left, Pressed = pressed },
            position);
    }

    /// <summary>
    /// Handles mouse input in 2D space
    /// </summary>
    /// <param name="event"></param>
    /// <param name="position"></param>
    private void HandleMouseInPosition(InputEventMouse @event, Vector3 position)
    {
        quadSize = (cameraViewDisplayMesh.Mesh as QuadMesh).Size;

        if (@event is InputEventMouseButton)
        {
            isMouseHeld = @event.IsPressed();
        }

        Vector3 mousePosition3D;

        if (isMouseInside)
        {
            mousePosition3D = uiArea.GlobalTransform.AffineInverse() * position;
            lastMousePosition3D = mousePosition3D;
        }
        else
        {
            mousePosition3D = lastMousePosition3D ?? Vector3.Zero;
        }

        var mousePosition2D = new Vector2(mousePosition3D.X, -mousePosition3D.Y);

        mousePosition2D.X += quadSize.X / 2;
        mousePosition2D.Y += quadSize.Y / 2;

        mousePosition2D.X /= quadSize.X;
        mousePosition2D.Y /= quadSize.Y;

        mousePosition2D.X *= subViewport.Size.X;
        mousePosition2D.Y *= subViewport.Size.Y;

        @event.Position = mousePosition2D;
        @event.GlobalPosition = mousePosition2D;

        if (@event is InputEventMouseMotion)
        {
            (@event as InputEventMouseMotion).Relative = mousePosition2D - lastMousePosition2D;
        }

        lastMousePosition2D = mousePosition2D;

        subViewport.PushInput(@event);
    }

    private bool FindMouse(Vector2 globalPosition, out Vector3 position)
    {
        var camera = GetViewport().GetCamera3D();

        var from = camera.ProjectRayOrigin(globalPosition);
        var dist = FindFurtherDistanceTo(camera.Transform.Origin);
        var to = from + camera.ProjectRayNormal(globalPosition) * dist;

        var parameters = new PhysicsRayQueryParameters3D()
        {
            From = from,
            To = to,
            CollideWithAreas = true,
            CollisionMask = uiArea.CollisionLayer,
            CollideWithBodies = false
        };

        var result = GetWorld3D().DirectSpaceState.IntersectRay(parameters);

        position = Vector3.Zero;

        var didFindMouse = result.Count > 0;
        if (didFindMouse)
        {
            position = (Vector3)result["position"];
        }

        return didFindMouse;
    }

    private float FindFurtherDistanceTo(Vector3 origin)
    {
        var edges = new Vector3[]
        {
            uiArea.ToGlobal(new Vector3(quadSize.X / 2, quadSize.Y / 2, 0)),
            uiArea.ToGlobal(new Vector3(quadSize.X / 2, -quadSize.Y / 2, 0)),
            uiArea.ToGlobal(new Vector3(-quadSize.X / 2, quadSize.Y / 2, 0)),
            uiArea.ToGlobal(new Vector3(-quadSize.X / 2, -quadSize.Y / 2, 0)),
        };

        float farDistance = 0;

        foreach (var edge in edges)
        {
            var tempDistance = origin.DistanceTo(edge);
            farDistance = Mathf.Max(farDistance, tempDistance);
        }

        return farDistance;
    }
}
