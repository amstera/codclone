using Mirror;
using UnityEngine;

public class NM : NetworkManager
{
    public Camera MainCamera;
    public Canvas Canvas;
	public Canvas TitleCanvas;

	override public void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        MainCamera.enabled = false;
        Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        TitleCanvas.enabled = false;
    }

    override public void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        OnDisconnect();
    }

    override public void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        if (conn.connectionId == 0)
        {
            OnDisconnect();
        }
    }

    private void OnDisconnect()
    {
        MainCamera.enabled = true;
        Canvas.renderMode = RenderMode.WorldSpace;
        TitleCanvas.enabled = true;
        Cursor.lockState = CursorLockMode.None;
    }
}
