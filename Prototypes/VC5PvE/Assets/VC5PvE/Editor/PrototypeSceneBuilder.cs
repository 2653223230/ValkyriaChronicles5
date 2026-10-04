using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace VC5PvE.Editor
{
    /// <summary>Creates the two self-contained prototype scenes and their imported art references.</summary>
    public static class PrototypeSceneBuilder
    {
        private const string ScenesFolder = "Assets/VC5PvE/Scenes";
        private const string GeneratedFolder = "Assets/Art/Generated";
        private const string PrefabsFolder = GeneratedFolder + "/Prefabs";
        private const string MaterialsFolder = GeneratedFolder + "/Materials";
        private const string SettingsFolder = "Assets/Settings";
        private const string TitleScenePath = ScenesFolder + "/Title.unity";
        private const string BattleScenePath = ScenesFolder + "/ForestRuins.unity";
        private const string CharacterSheet = "Assets/Art/External/CC0_RPGCharacters/RPGCharacterSprites32x32.png";
        private const string TitleImage = "Assets/Art/Generated/daylight-forest.png";
        private const string UiFontPath = "Assets/Art/External/Fonts/NotoSansCJKsc-Regular.otf";
        private const string ModelFolder = "Assets/Art/External/KenneyNatureKit/Models/FBX format/";
        private const string RendererPath = SettingsFolder + "/VC5PvE_UniversalRenderer.asset";
        private const string PipelinePath = SettingsFolder + "/VC5PvE_UniversalRenderPipeline.asset";

        private static readonly string[] TreeSources =
        {
            "tree_pineRoundA.fbx", "tree_pineDefaultA.fbx", "tree_oak_dark.fbx", "tree_small_dark.fbx"
        };

        private static readonly string[] RockSources =
        {
            "rock_largeA.fbx", "rock_largeB.fbx", "rock_tallA.fbx"
        };

        private static readonly string[] ShrubSources =
        {
            "plant_bush.fbx", "plant_bushLarge.fbx", "grass_leafs.fbx"
        };

        /// <summary>Idempotently imports prototype art, creates scenes, and registers them for this project only.</summary>
        public static void BuildScenes()
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EnsureFolder(ScenesFolder);
            EnsureFolder(GeneratedFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(MaterialsFolder);
            EnsureFolder(SettingsFolder);

            ConfigureSprite(TitleImage, false, false, 100f);

            Sprite[] unitSprites = LoadUnitSprites();
            Sprite titleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TitleImage);
            Font uiFont = AssetDatabase.LoadAssetAtPath<Font>(UiFontPath);
            if (titleSprite == null) throw new InvalidOperationException("Title background sprite could not be imported: " + TitleImage);
            if (uiFont == null) Debug.LogWarning("Noto Sans CJK SC did not import as a legacy Unity Font; PrototypeFlow will use its Arial fallback.");

            GameObject[] trees = CreatePrefabSet(TreeSources, 2.1f, "Tree");
            GameObject[] rocks = CreatePrefabSet(RockSources, 1.8f, "Rock");
            GameObject[] shrubs = CreatePrefabSet(ShrubSources, 1.25f, "Shrub");
            AudioClip cardCue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/KenneyRPGAudio/OGG/bookFlip1.ogg");
            AudioClip[] sounds = cardCue != null ? new[] { cardCue } : new AudioClip[0];

            UniversalRenderPipelineAsset pipeline = ConfigureUrpAndShaderRetention();
            BuildTitleScene(unitSprites, uiFont, titleSprite, trees, rocks, shrubs, sounds, pipeline);
            BuildBattleScene(unitSprites, uiFont, titleSprite, trees, rocks, shrubs, sounds, pipeline);
            var battleFlow=UnityEngine.Object.FindObjectOfType<PrototypeFlow>();
            battleFlow.RuinPrefabs=CreatePrefabSet(new[] {"wall-arch.fbx", "wall-broken.fbx", "pillar-stone.fbx"}, 3f, "Ruin", "Assets/Art/External/KenneyFantasyTownKit/Models/FBX format/");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), BattleScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TitleScenePath, true),
                new EditorBuildSettingsScene(BattleScenePath, true)
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            Debug.Log("VC5PvE scenes rebuilt: Title and ForestRuins. Active scene is Title.");
        }

        private static void BuildTitleScene(Sprite[] sprites, Font font, Sprite title, GameObject[] trees,
            GameObject[] rocks, GameObject[] shrubs, AudioClip[] sounds, UniversalRenderPipelineAsset pipeline)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "Title";
            ApplySceneLighting(pipeline);
            CreateTitleCamera();
            CreateDirectionalLight();
            CreateFlow(true, sprites, font, title, trees, rocks, shrubs, sounds);
            EditorSceneManager.SaveScene(scene, TitleScenePath);
        }

        private static void BuildBattleScene(Sprite[] sprites, Font font, Sprite title, GameObject[] trees,
            GameObject[] rocks, GameObject[] shrubs, AudioClip[] sounds, UniversalRenderPipelineAsset pipeline)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "ForestRuins";
            ApplySceneLighting(pipeline);
            CreateBattleCamera();
            CreateDirectionalLight();
            CreateFlow(false, sprites, font, title, trees, rocks, shrubs, sounds);
            EditorSceneManager.SaveScene(scene, BattleScenePath);
        }

        private static void CreateFlow(bool isTitle, Sprite[] sprites, Font font, Sprite title,
            GameObject[] trees, GameObject[] rocks, GameObject[] shrubs, AudioClip[] sounds)
        {
            var flowObject = new GameObject("Prototype Flow");
            var flow = flowObject.AddComponent<PrototypeFlow>();
            flow.IsTitle = isTitle;
            flow.UiFont = font;
            flow.TitleBackground = title;
            flow.GroundAtlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Generated/daylight-terrain.png");
            flow.ScenerySprites=LoadScenerySprites();
            flow.UnitSprites = sprites;
            flow.TreePrefabs = trees;
            flow.RockPrefabs = rocks;
            flow.ShrubPrefabs = shrubs;
            flow.SoundClips = sounds;
        }

        private static void CreateTitleCamera()
        {
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.015f, .045f, .055f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateBattleCamera()
        {
            // From the player's deployment edge, so rows 7–8 remain nearest and the HUD's middle column stays clear.
            Vector3 target = new Vector3(.1f, -.55f, -.05f);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = target + new Vector3(10f, 11f, -10f);
            cameraObject.transform.rotation = Quaternion.LookRotation(target - cameraObject.transform.position, Vector3.up);
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.63f, .79f, .78f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 6.35f;
            camera.nearClipPlane = .1f;
            camera.farClipPlane = 100f;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            var additional=cameraObject.AddComponent<UniversalAdditionalCameraData>();
            additional.renderPostProcessing=true;
            additional.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            ConfigureAtmosphere();
            CreateScenicBackdrop(camera);
        }

        private static void CreateDirectionalLight()
        {
            var lightObject = new GameObject("Directional Light", typeof(Light));
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, .94f, .79f);
            light.intensity = 1.05f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = .52f;
            lightObject.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
        }

        private static void ApplySceneLighting(UniversalRenderPipelineAsset pipeline)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.72f, .85f, .91f);
            RenderSettings.ambientEquatorColor = new Color(.47f, .58f, .53f);
            RenderSettings.ambientGroundColor = new Color(.23f, .29f, .26f);
            RenderSettings.reflectionIntensity = .45f;
        }

        private static Sprite[] LoadUnitSprites()
        {
            string[] names = { "frontliner", "flanker", "rifleman", "commander", "sniper" };
            var result = new Sprite[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string path="Assets/Art/LegacyStandees/"+names[i]+".png";
                ConfigureSprite(path,false,false,768f);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
                settings.spriteAlignment=(int)SpriteAlignment.BottomCenter;
                settings.spriteMeshType=SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);importer.maxTextureSize=2048;
                importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                result[i]=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(result[i]==null) throw new InvalidOperationException("Standee missing: "+path);
            }
            return result;
        }

        private static Sprite[] LoadScenerySprites()
        {
            const string path="Assets/Art/Generated/daylight-props.png";
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit=512f;importer.alphaIsTransparency=true;
            importer.wrapMode=TextureWrapMode.Clamp;
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            float[] edges={0f,.307f,.532f,.744f,1f};
            var frames=new SpriteMetaData[4];
            for(int i=0;i<4;i++) frames[i]=new SpriteMetaData {
                name="Scenery"+i,alignment=(int)SpriteAlignment.Custom,pivot=new Vector2(.5f,.135f),
                rect=new Rect(edges[i]*texture.width,0,(edges[i+1]-edges[i])*texture.width,texture.height)
            };
            importer.spritesheet=frames;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            // Unity 2021's legacy spritesheet setter can assign fileID 0 to the first sprite.
            // Give each subasset a stable nonzero identity before importing and saving scenes.
            var serialized=new SerializedObject(importer);
            var slices=serialized.FindProperty("m_SpriteSheet.m_Sprites");
            var ids=serialized.FindProperty("m_SpriteSheet.m_NameFileIdTable");
            ids.arraySize=4;
            for(int i=0;i<4;i++)
            {
                long id=21301000L+i;
                slices.GetArrayElementAtIndex(i).FindPropertyRelative("m_InternalID").longValue=id;
                slices.GetArrayElementAtIndex(i).FindPropertyRelative("m_SpriteID").stringValue=id.ToString("x32");
                ids.GetArrayElementAtIndex(i).FindPropertyRelative("first").stringValue="Scenery"+i;
                ids.GetArrayElementAtIndex(i).FindPropertyRelative("second").longValue=id;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();importer.SaveAndReimport();
            var result=new Sprite[4];
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if(asset is Sprite) for(int i=0;i<4;i++) if(asset.name=="Scenery"+i) result[i]=(Sprite)asset;
            return result;
        }

        private static void ConfigureAtmosphere()
        {
            const string profilePath=SettingsFolder+"/DaylightVolume.asset";
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,profilePath);}
            Bloom bloom;if(!profile.TryGet(out bloom)){bloom=profile.Add<Bloom>(true);AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.intensity.Override(.22f);bloom.threshold.Override(1.1f);bloom.scatter.Override(.45f);
            ColorAdjustments color;if(!profile.TryGet(out color)){color=profile.Add<ColorAdjustments>(true);AssetDatabase.AddObjectToAsset(color,profile);}
            color.postExposure.Override(0f);color.contrast.Override(3f);color.saturation.Override(-5f);
            Tonemapping tone;if(!profile.TryGet(out tone)){tone=profile.Add<Tonemapping>(true);AssetDatabase.AddObjectToAsset(tone,profile);}
            tone.mode.Override(TonemappingMode.Neutral);
            Vignette vignette;if(!profile.TryGet(out vignette)){vignette=profile.Add<Vignette>(true);AssetDatabase.AddObjectToAsset(vignette,profile);}
            vignette.intensity.Override(.12f);vignette.smoothness.Override(.5f);
            var go=new GameObject("Daylight atmosphere");var volume=go.AddComponent<Volume>();volume.isGlobal=true;volume.sharedProfile=profile;
            EditorUtility.SetDirty(profile);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=new Color(.66f,.8f,.78f);RenderSettings.fogStartDistance=22f;RenderSettings.fogEndDistance=43f;
        }

        private static void CreateScenicBackdrop(Camera camera)
        {
            const string path=MaterialsFolder+"/DaylightBackdrop.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("VC5PvE/ScenicBackdrop"));AssetDatabase.CreateAsset(material,path);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(TitleImage));
            var backdrop=GameObject.CreatePrimitive(PrimitiveType.Quad);backdrop.name="Painted distant forest";
            UnityEngine.Object.DestroyImmediate(backdrop.GetComponent<Collider>());
            backdrop.transform.SetParent(camera.transform,false);backdrop.transform.localPosition=new Vector3(0,0,48);
            backdrop.transform.localScale=new Vector3(camera.orthographicSize*2*16/9,camera.orthographicSize*2,1);
            backdrop.GetComponent<MeshRenderer>().sharedMaterial=material;
            backdrop.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
            backdrop.AddComponent<ScenicBackdrop>();EditorUtility.SetDirty(material);
        }

        private static void ConfigureSprite(string path, bool sliceCharacters, bool pointFilter, float pixelsPerUnit)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Texture importer is missing for " + path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = sliceCharacters ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = pointFilter ? FilterMode.Point : FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = !sliceCharacters;
            importer.wrapMode = TextureWrapMode.Clamp;
            if (sliceCharacters)
            {
                int[] rows = { 4, 11, 6, 1, 2 };
                string[] names = { "Warrior", "Mage", "Support", "Guard", "Shooter" };
                var slices = new SpriteMetaData[rows.Length];
                for (int i = 0; i < rows.Length; i++)
                {
                    slices[i] = new SpriteMetaData
                    {
                        name = "VC5 " + names[i],
                        rect = new Rect(0f, 672f - (rows[i] + 1) * 32f, 32f, 32f),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(.5f, 0f)
                    };
                }
                importer.spritesheet = slices;
            }
            importer.SaveAndReimport();
        }

        private static GameObject[] CreatePrefabSet(string[] filenames, float normalizedHeight, string kind, string modelFolder = ModelFolder)
        {
            var result = new GameObject[filenames.Length];
            for (int i = 0; i < filenames.Length; i++)
            {
                string sourcePath = modelFolder + filenames[i];
                string prefabPath = PrefabsFolder + "/" + kind + "_" + Path.GetFileNameWithoutExtension(filenames[i]) + ".prefab";
                var sourceAsset = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
                if (sourceAsset == null) throw new FileNotFoundException("Kenney FBX was not imported: " + sourcePath);

                var source = PrefabUtility.InstantiatePrefab(sourceAsset) as GameObject;
                if (source == null) throw new InvalidOperationException("Could not instantiate Kenney FBX: " + sourcePath);
                Bounds bounds;
                if (!TryCombinedBounds(source, out bounds) || bounds.size.y < .001f)
                {
                    UnityEngine.Object.DestroyImmediate(source);
                    throw new InvalidOperationException("FBX has no visible render bounds: " + sourcePath);
                }

                Vector3 sourceScale = source.transform.localScale;
                Vector3 sourceRotation = source.transform.localEulerAngles;
                float dimension = kind == "Tree" ? bounds.size.y : Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                float scale = normalizedHeight / dimension;
                var root = new GameObject(kind + " " + Path.GetFileNameWithoutExtension(filenames[i]));
                source.transform.SetParent(root.transform, false);
                source.transform.localScale = Vector3.Scale(sourceScale, Vector3.one * scale);
                source.transform.localEulerAngles = sourceRotation;
                source.transform.localPosition = new Vector3(-bounds.center.x * scale, -bounds.min.y * scale, -bounds.center.z * scale);
                ConvertRendererMaterials(source, kind, Path.GetFileNameWithoutExtension(filenames[i]));

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                UnityEngine.Object.DestroyImmediate(root);
                if (prefab == null) throw new InvalidOperationException("Could not save generated prefab: " + prefabPath);
                result[i] = prefab;
            }
            return result;
        }

        private static void ConvertRendererMaterials(GameObject model, string kind, string modelName)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
            if (litShader == null) throw new InvalidOperationException("URP/Lit shader is unavailable while generating Kenney materials.");
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Renderer renderer = renderers[rendererIndex];
                Material[] imported = renderer.sharedMaterials;
                var converted = new Material[imported.Length];
                for (int slot = 0; slot < imported.Length; slot++)
                {
                    Material source = imported[slot];
                    string sourceName = source == null ? "Default" : SafeAssetName(source.name);
                    string materialPath = MaterialsFolder + "/" + kind + "_" + modelName + "_" +
                        rendererIndex.ToString("00") + "_" + slot.ToString("00") + "_" + sourceName + ".mat";
                    converted[slot] = CreateUrpMaterial(source, materialPath, litShader);
                }
                renderer.sharedMaterials = converted;
            }
        }

        private static Material CreateUrpMaterial(Material source, string assetPath, Shader litShader)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(litShader);
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else material.shader = litShader;

            Color baseColor = Color.white;
            if (source != null)
            {
                if (source.HasProperty("_BaseColor")) baseColor = source.GetColor("_BaseColor");
                else if (source.HasProperty("_Color")) baseColor = source.GetColor("_Color");
            }
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", baseColor);

            Texture mainTexture = null;
            if (source != null)
            {
                if (source.HasProperty("_BaseMap")) mainTexture = source.GetTexture("_BaseMap");
                if (mainTexture == null && source.HasProperty("_MainTex")) mainTexture = source.GetTexture("_MainTex");
            }
            if (mainTexture != null && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", mainTexture);
                string textureProperty = source.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                material.SetTextureOffset("_BaseMap", source.GetTextureOffset(textureProperty));
                material.SetTextureScale("_BaseMap", source.GetTextureScale(textureProperty));
            }
            if (source != null && material.HasProperty("_Metallic") && source.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", source.GetFloat("_Metallic"));
            if (source != null && material.HasProperty("_Smoothness"))
            {
                if (source.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", source.GetFloat("_Smoothness"));
                else if (source.HasProperty("_Glossiness")) material.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static string SafeAssetName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Material";
            char[] characters = name.ToCharArray();
            for (int i = 0; i < characters.Length; i++)
                if (!char.IsLetterOrDigit(characters[i]) && characters[i] != '-' && characters[i] != '_') characters[i] = '_';
            return new string(characters);
        }

        private static bool TryCombinedBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = new Bounds();
            bool hasBounds = false;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled) continue;
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return hasBounds;
        }

        private static UniversalRenderPipelineAsset ConfigureUrpAndShaderRetention()
        {
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;

            // Retain the runtime firefly keyword without forcing every Lit variant into the player.
            pipeline.supportsHDR=true;pipeline.msaaSampleCount=4;pipeline.shadowDistance=40f;
            EnsureFolder("Assets/Resources");
            const string emissionPath = "Assets/Resources/VC5PvEEmission.mat";
            var emission = AssetDatabase.LoadAssetAtPath<Material>(emissionPath);
            if (emission == null)
            {
                emission = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(emission, emissionPath);
            }
            emission.EnableKeyword("_EMISSION");
            emission.SetColor("_EmissionColor", new Color(1f, .48f, .08f) * 1.7f);
            EditorUtility.SetDirty(emission);

            var requiredShaders = new[]
            {
                Shader.Find("VC5PvE/PixelCharacter"),
                Shader.Find("Sprites/Default"),
                Shader.Find("Universal Render Pipeline/Unlit")
            };
            UnityEngine.Object[] settingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (settingsAssets == null || settingsAssets.Length == 0 || settingsAssets[0] == null)
                throw new InvalidOperationException("Unity did not expose ProjectSettings/GraphicsSettings.asset to AssetDatabase.");
            var settings = new SerializedObject(settingsAssets[0]);
            SerializedProperty retainedShaders = settings.FindProperty("m_AlwaysIncludedShaders");
            if (retainedShaders == null || !retainedShaders.isArray)
                throw new InvalidOperationException("GraphicsSettings.asset does not expose the expected m_AlwaysIncludedShaders array.");
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            for (int i = retainedShaders.arraySize - 1; i >= 0; i--)
            {
                if (retainedShaders.GetArrayElementAtIndex(i).objectReferenceValue != lit) continue;
                int previousSize = retainedShaders.arraySize;
                retainedShaders.DeleteArrayElementAtIndex(i);
                if (retainedShaders.arraySize == previousSize) retainedShaders.DeleteArrayElementAtIndex(i);
            }
            foreach (Shader shader in requiredShaders)
            {
                if (shader == null) continue;
                bool found = false;
                for (int i = 0; i < retainedShaders.arraySize; i++)
                    if (retainedShaders.GetArrayElementAtIndex(i).objectReferenceValue == shader) { found = true; break; }
                if (found) continue;
                retainedShaders.InsertArrayElementAtIndex(retainedShaders.arraySize);
                retainedShaders.GetArrayElementAtIndex(retainedShaders.arraySize - 1).objectReferenceValue = shader;
            }
            settings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            EditorUtility.SetDirty(rendererData);
            EditorUtility.SetDirty(pipeline);
            return pipeline;
        }

        private static void EnsureFolder(string projectRelativePath)
        {
            string[] parts = projectRelativePath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
