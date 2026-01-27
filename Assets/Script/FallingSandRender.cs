using Pixel;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class FallingSandRender : MonoBehaviour
{
    public static FallingSandRender Instance { get; private set; }
    SpriteRenderer sr;
    Texture2D tex;

    public int pixelPerUnit = 100;
    public Texture2D Tex => tex;    
    
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    private void Start()
    {
        var fsw = FallingSandWorld.Instance;
        sr = GetComponent<SpriteRenderer>();
        tex = new(fsw.WorldWidth, fsw.WorldHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };        
        sr.sprite = Sprite.Create(tex, new(0, 0, fsw.WorldWidth, fsw.WorldHeight), new(0.5f, 0.5f), pixelPerUnit);
    }    
}
