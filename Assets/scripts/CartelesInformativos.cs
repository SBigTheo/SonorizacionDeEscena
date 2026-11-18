using UnityEngine;
using System.Text.RegularExpressions;

internal static class CartelesInformativos
{
    private static readonly Color Borde = new(1f, 0.32f, 0.25f, 0.9f);
    private static readonly Color Fondo = new(0.72f, 0.025f, 0.05f, 0.93f);
    private static readonly Color SombraTexto = new(0.05f, 0.05f, 0.06f, 0.85f);
    private static readonly GUIContent Contenido = new();
    private static readonly Regex Controles = new(@"[Cc]lic (?:derecho|izquierdo)|\b[EIRQ]\b");

    public static GUIStyle CrearTexto() => new(GUI.skin.label)
    {
        alignment = TextAnchor.MiddleCenter,
        normal = { textColor = Color.white }
    };

    public static void DibujarFondo(Rect rect)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        var color = GUI.color;
        GUI.color = Color.white;
        Rellenar(rect, Fondo, 4f);
        Rellenar(new Rect(rect.x + 4f, rect.y, rect.width - 8f, 1f), Borde);
        Rellenar(new Rect(rect.x + 4f, rect.yMax - 1f, rect.width - 8f, 1f), Borde);
        Rellenar(new Rect(rect.x, rect.y + 4f, 1f, rect.height - 8f), Borde);
        Rellenar(new Rect(rect.xMax - 1f, rect.y + 4f, 1f, rect.height - 8f), Borde);
        GUI.color = color;
    }

    private static void Rellenar(Rect rect, Color color, float radio = 0f)
        => GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, radio);

    public static void Dibujar(Rect rect, string texto, GUIStyle estilo)
    {
        DibujarFondo(rect);
        estilo.richText = true;
        DibujarTexto(rect, ResaltarControles(texto), estilo, Color.white);
    }

    public static string ResaltarControles(string texto) =>
        Controles.Replace(texto, "<color=#FFD644><b>$0</b></color>");

    public static void DibujarTexto(Rect rect, string texto, GUIStyle estilo, Color color)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        var colorGui = GUI.color;
        var colorContenido = GUI.contentColor;
        GUI.color = Color.white;
        GUI.contentColor = Color.white;
        estilo.alignment = TextAnchor.MiddleCenter;
        Contenido.text = texto;
        estilo.normal.textColor = SombraTexto;
        var sombra = new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height);
        estilo.Draw(sombra, Contenido, false, false, false, false);
        estilo.normal.textColor = color;
        estilo.Draw(rect, Contenido, false, false, false, false);
        GUI.color = colorGui;
        GUI.contentColor = colorContenido;
    }
}
