// Core/AssetsManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NordGuide.Core {
    /// <summary>
    /// Carrega PNGs embutidos no .dll (EmbeddedResource) e expõe como Texture2D / Sprite.
    /// Sem dependência de arquivos externos; compatível com Thunderstore.
    /// </summary>
    internal static class AssetsManager {
        private static readonly Dictionary<string, Texture2D> _texByKey =
            new Dictionary<string, Texture2D>( StringComparer.OrdinalIgnoreCase );

        private static readonly Dictionary<string, Sprite> _sprByKey =
            new Dictionary<string, Sprite>( StringComparer.OrdinalIgnoreCase );

        private static bool _loaded;

        // (compat) alguns Awakes chamam Initialize(); deixamos no-op
        public static void Initialize() { /* no-op: carregamos sob demanda */ }

        public static Texture2D Tex( string nameOrFile ) {
            EnsureLoaded();
            return TryGet( _texByKey, NormalizeKey( nameOrFile ) );
        }

        public static Sprite Sprite( string nameOrFile ) {
            EnsureLoaded();
            return TryGet( _sprByKey, NormalizeKey( nameOrFile ) );
        }

        public static IEnumerable<string> Keys() {
            EnsureLoaded();
            return _texByKey.Keys.OrderBy( k => k );
        }

        private static void EnsureLoaded() {
            if (_loaded)
                return;
            _loaded = true;

            var asm = Assembly.GetExecutingAssembly();
            var resources = asm.GetManifestResourceNames(); // ex.: NordGuide.Assets.compass_bar.png

            foreach (var res in resources) {
                if (!res.EndsWith( ".png", StringComparison.OrdinalIgnoreCase ))
                    continue;

                var key = NormalizeKey( Path.GetFileName( res ) );
                if (string.IsNullOrEmpty( key ))
                    continue;

                try {
                    using (var s = asm.GetManifestResourceStream( res )) {
                        if (s == null)
                            continue;

                        using (var ms = new MemoryStream()) {
                            s.CopyTo( ms );
                            var bytes = ms.ToArray();

                            var tex = new Texture2D( 2, 2, TextureFormat.RGBA32, false );

                            // Carrega PNG via reflexão (sem referenciar ImageConversionModule no build)
                            if (!LoadImageCompat( tex, bytes, markNonReadable: false )) {
                                NordGuide.Log?.LogWarning( $"[Assets] ImageConversion.LoadImage indisponível: {res}" );
                                continue;
                            }

                            tex.wrapMode = TextureWrapMode.Clamp;
                            tex.filterMode = FilterMode.Bilinear;

                            _texByKey[key] = tex;

                            var spr = UnityEngine.Sprite.Create(
                                tex,
                                new Rect( 0, 0, tex.width, tex.height ),
                                new Vector2( 0.5f, 0.5f ),
                                100f,
                                0,
                                SpriteMeshType.FullRect
                            );

                            _sprByKey[key] = spr;
                        }
                    }
                } catch (Exception e) {
                    NordGuide.Log?.LogWarning( $"[Assets] Falha ao carregar resource: {res} -> {e.Message}" );
                }
            }

            NordGuide.Log?.LogInfo( $"[Assets] Carregados {_texByKey.Count} textures / {_sprByKey.Count} sprites embutidos." );
            #if DEBUG
            try
            {
                var keys = string.Join(", ", _texByKey.Keys.OrderBy(k => k));
                NordGuide.Log?.LogInfo($"[Assets] Keys: {keys}");
            }
            catch { }
            #endif
        }

        // Chama UnityEngine.ImageConversion.LoadImage(Texture2D, byte[], bool) por reflexão
        private static bool LoadImageCompat( Texture2D tex, byte[] data, bool markNonReadable ) {
            try {
                var t = Type.GetType( "UnityEngine.ImageConversion, UnityEngine.ImageConversionModule", throwOnError: false );
                if (t == null)
                    return false;

                var mi = t.GetMethod(
                    "LoadImage",
                    BindingFlags.Public | BindingFlags.Static,
                    binder: null,
                    types: new[] { typeof( Texture2D ), typeof( byte[] ), typeof( bool ) },
                    modifiers: null
                );

                if (mi == null)
                    return false;
                return (bool)mi.Invoke( null, new object[] { tex, data, markNonReadable } );
            } catch (Exception e) {
                NordGuide.Log?.LogWarning( $"[Assets] LoadImageCompat falhou: {e.Message}" );
                return false;
            }
        }

        private static string NormalizeKey( string nameOrFile ) {
            if (string.IsNullOrWhiteSpace( nameOrFile ))
                return null;

            // tira extensão .png (se houver)
            var s = nameOrFile.Trim();
            if (s.EndsWith( ".png", StringComparison.OrdinalIgnoreCase ))
                s = s.Substring( 0, s.Length - 4 );

            // se for um resource name (ex.: "NordGuide.Assets.PinN_g"),
            // pegamos APENAS a última parte após o último '.'
            int lastDot = s.LastIndexOf( '.' );
            if (lastDot >= 0 && lastDot + 1 < s.Length)
                s = s.Substring( lastDot + 1 );

            return s;
        }

        private static T TryGet<T>( Dictionary<string, T> map, string key ) where T : class {
            if (key != null && map.TryGetValue( key, out var val ))
                return val;
            return null;
        }
    }
}