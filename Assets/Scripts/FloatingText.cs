using Mirror;
using UnityEngine;

public class FloatingText : NetworkBehaviour
{
    public float TotalTime = 2f;
    private float elapsedTime;
    private TextMesh textMesh;

    void Start()
    {
        textMesh = GetComponent<TextMesh>();
    }

    void Update()
    {
        if (elapsedTime < TotalTime)
        {
            transform.position += new Vector3(0, 0.01f, 0);

            Color color = textMesh.color;
            color.a -= 0.01f;
            textMesh.color = color;

            elapsedTime += Time.deltaTime;
        }
    }

    public void UpdateText(string text)
    {
        GetComponent<TextMesh>().text = text;
    }
}
