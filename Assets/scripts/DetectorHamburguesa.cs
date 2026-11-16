using System.Collections.Generic;
using UnityEngine;

public class DetectorHamburguesa : MonoBehaviour
{
    [Header("Prefabs requeridos (arrastra los 4)")]
    public GameObject panBajo;
    public GameObject panSuperior;
    public GameObject carne;
    public GameObject cheese;

    [Header("Resultado")]
    public GameObject hamburguesaCompleta; // El prefab de la hamburguesa ya armada
    public Transform puntoDeAparicion;     // Dónde aparece (opcional, si es null usa la madera)

    // Nombres exactos de los objetos que deben tocar la madera
    private readonly HashSet<string> ingredientesRequeridos = new HashSet<string>
    {
        "panbajo", "pansuperior", "carne", "cheese"
    };

    private HashSet<string> ingredientesPresentes = new HashSet<string>();
    private bool yaCompletada = false;

    private void OnTriggerEnter(Collider other)
    {
        if (yaCompletada) return;

        var agarrable = other.GetComponent<Agarrable>();
        if (agarrable != null && agarrable.estaAgarrado) return;

        // Usa el nombre del GameObject o una etiqueta. Aquí uso el nombre.
        string nombre = other.gameObject.name.ToLower().Trim();

        // Acepta variantes tipo "panbajo(Clone)" o "panbajo (1)"
        foreach (var requerido in ingredientesRequeridos)
        {
            if (nombre.StartsWith(requerido))
            {
                ingredientesPresentes.Add(requerido);
                Debug.Log($"Ingrediente detectado: {requerido} ({ingredientesPresentes.Count}/4)");
                break;
            }
        }

        if (ingredientesPresentes.Count >= ingredientesRequeridos.Count)
            CompletarHamburguesa();
    }

    private void OnTriggerExit(Collider other)
    {
        if (yaCompletada) return;

        string nombre = other.gameObject.name.ToLower().Trim();
        foreach (var requerido in ingredientesRequeridos)
        {
            if (nombre.StartsWith(requerido))
            {
                ingredientesPresentes.Remove(requerido);
                break;
            }
        }
    }

    private void CompletarHamburguesa()
    {
        yaCompletada = true;
        Debug.Log("¡Hamburguesa completa!");

        // Opción A: instanciar la hamburguesa armada en el punto
        if (hamburguesaCompleta != null)
        {
            Vector3 pos = puntoDeAparicion != null ? puntoDeAparicion.position : transform.position;
            Instantiate(hamburguesaCompleta, pos, Quaternion.identity);
        }

        // Opción B: destruir los ingredientes sueltos (opcional)
        // GameObject.Find... o guardar referencias en OnTriggerEnter

        // Opción C: activar un GameObject que ya está en la escena
        // hamburguesaCompleta.SetActive(true);
    }
}