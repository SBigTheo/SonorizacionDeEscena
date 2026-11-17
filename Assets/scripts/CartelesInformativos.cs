using UnityEngine;

internal static class CartelesInformativos
{
    private static readonly Color Borde = new(0.82f, 0.83f, 0.85f, 0.62f);
    private static readonly Color Fondo = new(0.42f, 0.43f, 0.45f, 0.66f);
    private static readonly Color SombraTexto = new(0.05f, 0.05f, 0.06f, 0.85f);
    private static readonly GUIContent Contenido = new();

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
        DibujarTexto(rect, texto, estilo, Color.white);
    }

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
