using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Profile;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;

/// <summary>
/// Menú "VR > Configurar proyecto para Shinecon (Cardboard)": deja el proyecto listo para
/// compilar un APK que corre en un celular Android dentro de un visor tipo Shinecon, con el
/// plugin oficial de Google Cardboard. Sigue la guía
/// https://developers.google.com/cardboard/develop/unity/quickstart y se puede ejecutar
/// varias veces sin problema. Las dependencias de Gradle que pide Cardboard están en
/// Assets/Plugins/Android (mainTemplate.gradle y gradleTemplate.properties).
/// Requiere tener instalado en Unity Hub el módulo "Android Build Support" (con OpenJDK y
/// Android SDK &amp; NDK Tools).
/// </summary>
public static class ConfigurarCardboardVR
{
    private const string CarpetaXR = "Assets/XR";
    private const string RutaAjustesXR = CarpetaXR + "/XRGeneralSettingsPerBuildTarget.asset";
    private const string LoaderCardboard = "Google.XR.Cardboard.XRLoader";

    [MenuItem("VR/Configurar proyecto para Shinecon (Cardboard)")]
    public static void Configurar()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            EditorUtility.DisplayDialog("Falta el módulo Android",
                "Instala 'Android Build Support' (con OpenJDK y Android SDK & NDK Tools) para esta versión de Unity desde Unity Hub > Installs > Add modules, reinicia Unity y vuelve a ejecutar este menú.",
                "OK");
            return;
        }

        ConfigurarAndroid();
        ConfigurarXRManagement();
        ConfigurarURPParaCelular();

        ActivarAndroid();
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Listo",
            "Proyecto configurado para Shinecon (Google Cardboard).\n\nPara probar: conecta el celular Android por USB (con la depuración USB activada) y usa VR > Compilar APK, o File > Build Profiles > Android > Build And Run.",
            "OK");
    }

    /// <summary>
    /// Compila el APK con las escenas de Build Settings en "Builds/SedAlgoritmica.apk" (junto a
    /// la carpeta Assets). También se puede llamar por consola con
    /// -executeMethod ConfigurarCardboardVR.CompilarAPK
    /// </summary>
    [MenuItem("VR/Compilar APK")]
    public static void CompilarAPK()
    {
        // URP elige qué pipelines y shaders incluir según la plataforma ACTIVA del editor, no
        // según la del build: si seguía activa Mac, el APK salía con el pipeline de PC.
        ActivarAndroid();

        string[] escenas = EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
        var opciones = new BuildPlayerOptions
        {
            scenes = escenas,
            locationPathName = "Builds/SedAlgoritmica.apk",
            target = BuildTarget.Android,
            targetGroup = BuildTargetGroup.Android,
            options = BuildOptions.None,
        };

        var reporte = BuildPipeline.BuildPlayer(opciones);
        Debug.Log($"ConfigurarCardboardVR: build {reporte.summary.result}, {reporte.summary.totalErrors} errores, {reporte.summary.totalSize / (1024 * 1024)} MB -> {opciones.locationPathName}");

        if (Application.isBatchMode)
            EditorApplication.Exit(reporte.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }

    /// <summary>
    /// Deja Android como plataforma activa. El proyecto tiene un Build Profile "macOS"
    /// (Assets/Settings/Build Profiles) que Unity reactiva al abrir el proyecto si estaba
    /// activo, deshaciendo el cambio; por eso primero volvemos al perfil de plataforma normal.
    /// </summary>
    private static void ActivarAndroid()
    {
        BuildProfile activo = BuildProfile.GetActiveBuildProfile();
        if (activo != null)
        {
            Debug.Log($"ConfigurarCardboardVR: se desactiva el Build Profile '{activo.name}' para usar la plataforma Android.");
            BuildProfile.SetActiveBuildProfile(null);
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
    }

    /// <summary>Ajustes de Player Settings que pide la guía de Cardboard.</summary>
    private static void ConfigurarAndroid()
    {
        NamedBuildTarget android = NamedBuildTarget.Android;

        // El celular va acostado dentro del visor.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.Android.optimizedFramePacing = false;

        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)35;
        PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
        // Cardboard descarga los parámetros del visor al escanear su código QR.
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.colorSpace = ColorSpace.Linear;

        // OpenGL ES 3 es lo más compatible entre celulares de gama media y baja.
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

        // El identificador venía de la plantilla de Unity; Android necesita uno propio.
        string id = PlayerSettings.GetApplicationIdentifier(android);
        if (string.IsNullOrEmpty(id) || id.Contains("unity.template"))
            PlayerSettings.SetApplicationIdentifier(android, "com.concursocienciadatos.sedalgoritmica");
    }

    private static void ConfigurarXRManagement()
    {
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget porPlataforma);
        if (porPlataforma == null)
        {
            if (!AssetDatabase.IsValidFolder(CarpetaXR)) AssetDatabase.CreateFolder("Assets", "XR");

            porPlataforma = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(RutaAjustesXR);
            if (porPlataforma == null)
            {
                porPlataforma = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(porPlataforma, RutaAjustesXR);
            }
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, porPlataforma, true);
        }

        XRGeneralSettings ajustes = porPlataforma.SettingsForBuildTarget(BuildTargetGroup.Android);
        if (ajustes == null)
        {
            ajustes = ScriptableObject.CreateInstance<XRGeneralSettings>();
            ajustes.name = "Android Settings";
            porPlataforma.SetSettingsForBuildTarget(BuildTargetGroup.Android, ajustes);
            AssetDatabase.AddObjectToAsset(ajustes, porPlataforma);
        }

        if (ajustes.Manager == null)
        {
            XRManagerSettings manager = ScriptableObject.CreateInstance<XRManagerSettings>();
            manager.name = "Android Providers";
            ajustes.Manager = manager;
            AssetDatabase.AddObjectToAsset(manager, porPlataforma);
        }

        // Cardboard debe ser el único proveedor de VR en Android.
        foreach (var loader in ajustes.Manager.activeLoaders.ToArray())
        {
            if (loader != null && loader.GetType().FullName != LoaderCardboard)
                XRPackageMetadataStore.RemoveLoader(ajustes.Manager, loader.GetType().FullName, BuildTargetGroup.Android);
        }

        ajustes.InitManagerOnStart = true;
        if (!XRPackageMetadataStore.AssignLoader(ajustes.Manager, LoaderCardboard, BuildTargetGroup.Android))
            Debug.LogError("ConfigurarCardboardVR: no se pudo asignar el loader de Cardboard. ¿Está instalado el paquete com.google.xr.cardboard?");

        EditorUtility.SetDirty(ajustes);
        EditorUtility.SetDirty(porPlataforma);
    }

    /// <summary>
    /// Ajustes del asset URP que usa Android (nivel de calidad "Mobile"): un celular tiene que
    /// dibujar la escena dos veces (una por ojo), así que se apaga el HDR y se usa MSAA 2x.
    /// </summary>
    private static void ConfigurarURPParaCelular()
    {
        int nivelMovil = System.Array.IndexOf(QualitySettings.names, "Mobile");
        if (nivelMovil < 0) return;

        if (QualitySettings.GetRenderPipelineAssetAt(nivelMovil) is UniversalRenderPipelineAsset urp)
        {
            urp.supportsHDR = false;
            urp.msaaSampleCount = 2;
            urp.renderScale = 1f;
            EditorUtility.SetDirty(urp);
        }
    }
}
