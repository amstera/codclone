using Mirror;
using UnityEngine;

public class NM : NetworkManager
{
    public Camera MainCamera;
    public Canvas Canvas;

    override public void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        MainCamera.enabled = false;
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    }

    override public void OnClientDisconnect(NetworkConnection conn)
    {
        //check if local player
        base.OnClientDisconnect(conn);
        Canvas.renderMode = RenderMode.WorldSpace;
    }

    override public void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        MainCamera.enabled = true;
        Canvas.renderMode = RenderMode.WorldSpace;
    }
}
