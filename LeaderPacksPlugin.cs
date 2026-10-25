using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using UnityEngine;

namespace LeaderPacksAOC
{
    [BepInPlugin("josesito.leaderpacks", "Leader Packs", "0.1.0")]
    [BepInProcess("Ages of Conflict.exe")]
    public class LeaderPacksPlugin : BaseUnityPlugin
    {
        private bool open;
        private Rect window = new Rect(30, 30, 620, 590);
        private string search = "";
        private string country = "";
        private string customName = "";
        private string status = "Leader Packs loaded. Press F6.";
        private Vector2 listScroll;
        private Vector2 packScroll;
        private int packIndex;
        private int selectedIndex = -1;
        private Texture2D canvas;
        private float brush = 5f;
        private int colorIndex;
        private bool drawing;
        private Rect canvasRect;
        private string dataRoot;
        private string portraitsRoot;
        private string linksPath;

        private readonly string[] packNames =
        {
            "Antigüedad", "Edad Media", "Revolución francesa", "Independencias americanas",
            "Primera Guerra Mundial", "Segunda Guerra Mundial", "Personalizados"
        };

        private readonly Dictionary<string, string[]> leadersByPack = new Dictionary<string, string[]>
        {
            { "Antigüedad", new[] { "Julio César", "Alejandro Magno", "Cleopatra", "Augusto" } },
            { "Edad Media", new[] { "Carlomagno", "Saladino", "Juana de Arco", "Gengis Kan" } },
            { "Revolución francesa", new[] { "Napoleón Bonaparte", "Luis XVI", "Maximilien Robespierre" } },
            { "Independencias americanas", new[] { "Simón Bolívar", "José de San Martín", "Miguel Hidalgo", "Bernardo O'Higgins" } },
            { "Primera Guerra Mundial", new[] { "Guillermo II", "Nicolás II", "Woodrow Wilson", "Georges Clemenceau" } },
            { "Segunda Guerra Mundial", new[] { "Winston Churchill", "Franklin D. Roosevelt", "Charles de Gaulle", "Hideki Tojo" } },
            { "Personalizados", new string[0] }
        };

        private void Awake()
        {
            dataRoot = Path.Combine(Paths.PluginPath, "LeaderPacks");
            portraitsRoot = Path.Combine(dataRoot, "portraits");
            linksPath = Path.Combine(dataRoot, "country-leaders.tsv");
            Directory.CreateDirectory(portraitsRoot);

            canvas = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            ClearCanvas();
            Logger.LogInfo("Leader Packs loaded. Press F6.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6)) open = !open;
        }

        private void OnGUI()
        {
            if (open)
                window = GUI.Window(781026, window, DrawWindow, "Leader Packs — Ages of Conflict");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label("Paquetes de retratos históricos (catálogo inicial de nombres).");
            GUILayout.Label("Paquete:");
            packScroll = GUILayout.BeginScrollView(packScroll, GUILayout.Height(66));
            packIndex = GUILayout.SelectionGrid(packIndex, packNames, 3);
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Buscar líder:", GUILayout.Width(85));
            search = GUILayout.TextField(search ?? "", GUILayout.Width(190));
            GUILayout.Label("País:", GUILayout.Width(40));
            country = GUILayout.TextField(country ?? "", GUILayout.Width(170));
            GUILayout.EndHorizontal();

            string pack = packNames[Mathf.Clamp(packIndex, 0, packNames.Length - 1)];
            var all = leadersByPack.ContainsKey(pack) ? leadersByPack[pack] : new string[0];
            var filtered = all.Where(n => string.IsNullOrEmpty(search) ||
                n.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();

            GUILayout.Label("Líderes disponibles:");
            listScroll = GUILayout.BeginScrollView(listScroll, GUILayout.Height(90));
            if (filtered.Length == 0) GUILayout.Label("No hay coincidencias. Puedes dibujar uno abajo.");
            for (int i = 0; i < filtered.Length; i++)
            {
                if (GUILayout.Button(filtered[i], GUILayout.Width(260)))
                {
                    selectedIndex = Array.IndexOf(all, filtered[i]);
                    customName = filtered[i];
                    status = "Seleccionaste " + customName + ". Dibuja su retrato o guarda el nombre.";
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(3);
            GUILayout.Label("Nombre del líder personalizado:");
            customName = GUILayout.TextField(customName ?? "", 60);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pincel", GUILayout.Width(45));
            brush = GUILayout.HorizontalSlider(brush, 1f, 18f, GUILayout.Width(120));
            GUILayout.Label("Color", GUILayout.Width(40));
            colorIndex = GUILayout.SelectionGrid(colorIndex, new[] { "Negro", "Rojo", "Azul", "Verde", "Amarillo", "Blanco" }, 3, GUILayout.Width(250));
            GUILayout.EndHorizontal();

            GUILayout.Label("Lienzo 256 × 256:");
            Rect area = GUILayoutUtility.GetRect(256, 256, GUILayout.Width(256), GUILayout.Height(256));
            canvasRect = area;
            GUI.DrawTexture(area, canvas, ScaleMode.StretchToFill, false);
            HandleDrawing(area, Event.current);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Limpiar", GUILayout.Width(90))) ClearCanvas();
            if (GUILayout.Button("Guardar retrato", GUILayout.Width(130))) SaveLeader();
            if (GUILayout.Button("Asignar al país", GUILayout.Width(130))) LinkCountry();
            GUILayout.EndHorizontal();
            GUILayout.Label(status, GUILayout.Height(35));
            GUILayout.Label("Los archivos se guardan en BepInEx/plugins/LeaderPacks/");
            GUILayout.Label("Prototipo: guarda retratos y asignaciones; aún no cambia la interfaz interna del juego.");
            GUI.DragWindow(new Rect(0, 0, 10000, 22));
        }

        private void HandleDrawing(Rect area, Event e)
        {
            if (e == null) return;
            bool inside = area.Contains(e.mousePosition);
            if (e.type == EventType.MouseDown && e.button == 0 && inside)
            {
                drawing = true;
                Paint(e.mousePosition, area);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && drawing && e.button == 0)
            {
                if (inside) Paint(e.mousePosition, area);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                drawing = false;
            }
        }

        private void Paint(Vector2 mouse, Rect area)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt((mouse.x - area.x) / area.width * 255), 0, 255);
            int y = Mathf.Clamp(255 - Mathf.RoundToInt((mouse.y - area.y) / area.height * 255), 0, 255);
            int r = Mathf.Max(1, Mathf.RoundToInt(brush));
            Color c = SelectedColor();
            for (int dx = -r; dx <= r; dx++)
                for (int dy = -r; dy <= r; dy++)
                {
                    if (dx * dx + dy * dy > r * r) continue;
                    int px = x + dx, py = y + dy;
                    if (px >= 0 && px < 256 && py >= 0 && py < 256)
                        canvas.SetPixel(px, py, c);
                }
            canvas.Apply();
        }

        private Color SelectedColor()
        {
            switch (colorIndex)
            {
                case 1: return Color.red;
                case 2: return Color.blue;
                case 3: return Color.green;
                case 4: return Color.yellow;
                case 5: return Color.white;
                default: return Color.black;
            }
        }

        private void ClearCanvas()
        {
            if (canvas == null) return;
            Color[] pixels = new Color[256 * 256];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            canvas.SetPixels(pixels);
            canvas.Apply();
        }

        private void SaveLeader()
        {
            string name = (customName ?? "").Trim();
            if (name.Length == 0) { status = "Escribe el nombre del líder primero."; return; }
            try
            {
                string file = SafeFile(name) + ".png";
                File.WriteAllBytes(Path.Combine(portraitsRoot, file), canvas.EncodeToPNG());
                status = "Retrato guardado: " + file;
                Logger.LogInfo(status);
            }
            catch (Exception ex) { status = "Error al guardar: " + ex.Message; Logger.LogError(ex); }
        }

        private void LinkCountry()
        {
            string c = (country ?? "").Trim();
            string leader = (customName ?? "").Trim();
            if (c.Length == 0 || leader.Length == 0)
            {
                status = "Escribe el país y el nombre del líder.";
                return;
            }
            try
            {
                string existing = File.Exists(linksPath) ? File.ReadAllText(linksPath, Encoding.UTF8) : "";
                var lines = existing.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !line.Split('\t')[0].Equals(c, StringComparison.OrdinalIgnoreCase)).ToList();
                lines.Add(c.Replace("\t", " ") + "\t" + leader.Replace("\t", " "));
                File.WriteAllText(linksPath, string.Join(Environment.NewLine, lines) + Environment.NewLine, Encoding.UTF8);
                status = "Asignación guardada: " + c + " → " + leader;
            }
            catch (Exception ex) { status = "Error al asignar: " + ex.Message; Logger.LogError(ex); }
        }

        private string SafeFile(string value)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '_');
            return value.Trim();
        }

        private void OnDestroy()
        {
            if (canvas != null) Destroy(canvas);
        }
    }
}
