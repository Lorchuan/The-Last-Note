using TMPro;
using UnityEngine;
using System.Collections;

public class AncientText : MonoBehaviour
{
    public TextMeshPro textMesh;
    public Transform player;    
    public float showDistance = 5f;

    void Update()
    {
        if (player == null) player = Camera.main.transform;

        float dist = Vector3.Distance(transform.position, player.position);

        // 距离越近越清晰
        float alpha = Mathf.Clamp01(1 - (dist / showDistance));
        textMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, alpha);
    }
}