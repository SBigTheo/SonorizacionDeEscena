using StarterAssets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(ThirdPersonController))]
// Publica la pose de la cámara antes del LateUpdate de AkInitializer (-100).
[DefaultExecutionOrder(-110)]
public sealed class JugadorStarterAssets : MonoBehaviour
{
    private ThirdPersonController controlador;
    private Camera camara;
    private GameObject emisorPasos;
    private Mesh mallaPies;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        controlador = GetComponent<ThirdPersonController>();
        controlador.WorldScale = Mathf.Abs(transform.lossyScale.x);
        camara = controlador.CinemachineCameraTarget.GetComponentInChildren<Camera>();
        if (camara != null)
            CrearPiesEnPrimeraPersona();
    }

    private void OnEnable() => controlador.Footstep += ReproducirPisada;

    private void OnDisable() => controlador.Footstep -= ReproducirPisada;

    private void OnDestroy()
    {
        Destroy(mallaPies);
        Destroy(emisorPasos);
    }

    private void LateUpdate()
    {
        if (camara != null && camara.isActiveAndEnabled && AkUnitySoundEngine.IsInitialized())
            AkUnitySoundEngine.SetObjectPosition(camara.gameObject, camara.transform);
    }

    private void ReproducirPisada()
    {
        if (camara == null || !controlador.Grounded || !AkUnitySoundEngine.IsInitialized())
            return;

        if (emisorPasos == null)
        {
            emisorPasos = new GameObject("Pisadas del jugador");
            emisorPasos.transform.SetParent(camara.transform, false);
            emisorPasos.AddComponent<AkGameObj>();
            AkUnitySoundEngine.SetObjectPosition(emisorPasos, emisorPasos.transform);
            const float gananciaTresDb = 1.41254f;
            AkUnitySoundEngine.SetGameObjectOutputBusVolume(
                AkUnitySoundEngine.GetAkGameObjectID(emisorPasos),
                AkUnitySoundEngine.GetAkGameObjectID(camara.gameObject), gananciaTresDb);
        }
        AkUnitySoundEngine.PostEvent("Play_PasoJugador", emisorPasos);
    }

    private void CrearPiesEnPrimeraPersona()
    {
        var modelo = GetComponentInChildren<SkinnedMeshRenderer>();
        var animador = GetComponent<Animator>();
        var origen = modelo.sharedMesh;
        var izquierdo = animador.GetBoneTransform(HumanBodyBones.LeftFoot);
        var derecho = animador.GetBoneTransform(HumanBodyBones.RightFoot);
        var huesos = modelo.bones;
        var esHuesoDelPie = new bool[huesos.Length];
        for (var i = 0; i < huesos.Length; i++)
            esHuesoDelPie[i] = huesos[i] != null &&
                (huesos[i] == izquierdo || huesos[i] == derecho ||
                 huesos[i].IsChildOf(izquierdo) || huesos[i].IsChildOf(derecho));

        var pesos = origen.boneWeights;
        var esVerticeDelPie = new bool[pesos.Length];
        for (var i = 0; i < pesos.Length; i++)
        {
            var peso = pesos[i];
            var influencia = (esHuesoDelPie[peso.boneIndex0] ? peso.weight0 : 0f) +
                             (esHuesoDelPie[peso.boneIndex1] ? peso.weight1 : 0f) +
                             (esHuesoDelPie[peso.boneIndex2] ? peso.weight2 : 0f) +
                             (esHuesoDelPie[peso.boneIndex3] ? peso.weight3 : 0f);
            esVerticeDelPie[i] = influencia >= 0.5f;
        }

        mallaPies = Instantiate(origen);
        mallaPies.name = "Pies del jugador";
        for (var submalla = 0; submalla < origen.subMeshCount; submalla++)
        {
            var indices = origen.GetTriangles(submalla);
            var pies = new List<int>();
            for (var i = 0; i < indices.Length; i += 3)
            {
                if (!esVerticeDelPie[indices[i]] || !esVerticeDelPie[indices[i + 1]] ||
                    !esVerticeDelPie[indices[i + 2]])
                    continue;

                pies.Add(indices[i]);
                pies.Add(indices[i + 1]);
                pies.Add(indices[i + 2]);
            }
            mallaPies.SetTriangles(pies, submalla, false);
        }

        var objeto = new GameObject(mallaPies.name);
        objeto.transform.SetParent(modelo.transform, false);
        var renderer = objeto.AddComponent<SkinnedMeshRenderer>();
        renderer.sharedMesh = mallaPies;
        renderer.sharedMaterials = modelo.sharedMaterials;
        renderer.rootBone = modelo.rootBone;
        renderer.bones = huesos;
        renderer.localBounds = modelo.localBounds;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }
}
