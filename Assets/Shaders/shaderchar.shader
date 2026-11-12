Shader "Kelas11/Belajar/01_Warna"
{
    // PELAJARAN 1 — Shader paling sederhana: setiap piksel dapat warna yang sama.
    //
    // Shader = resep untuk GPU. C# mengatur "apa yang ada di dunia".
    // Shader mengatur "bagaimana benda itu digambar di layar".
    //
    // Cara pakai di Unity:
    // 1. Hierarchy > 2D Object > Sprites > Square
    // 2. Project window: klik kanan shader ini > Create > Material
    // 3. Di material, pastikan Shader = Kelas11/Belajar/01_Warna
    // 4. Drag material ke Sprite Renderer (slot Material)
    // 5. Ubah "Warna" di Inspector — layar ikut berubah, tanpa C#.

    Properties
    {
        _Warna ("Warna", Color) = (1, 0.35, 0.2, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "Unlit2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Warna;
            CBUFFER_END

            struct Atribut
            {
                float4 posisiObjek : POSITION;
            };

            struct KeFragment
            {
                float4 posisiClip : SV_POSITION;
            };

            KeFragment Vert(Atribut masuk)
            {
                KeFragment keluar;
                // Object space -> clip space (posisi di layar).
                keluar.posisiClip = TransformObjectToHClip(masuk.posisiObjek.xyz);
                return keluar;
            }

            half4 Frag(KeFragment masuk) : SV_Target
            {
                return (half4)_Warna;
            }
            ENDHLSL
        }
    }
}
