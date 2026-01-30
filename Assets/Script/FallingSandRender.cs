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
        var worldConfig = fsw.WorldConfig;        
        tex = new(worldConfig.width, worldConfig.height, TextureFormat.RGBA32, false,true)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };        
        sr.sprite = Sprite.Create(tex, new(0, 0, worldConfig.width,worldConfig.height), new(0.5f, 0.5f), pixelPerUnit);
    }    
}
