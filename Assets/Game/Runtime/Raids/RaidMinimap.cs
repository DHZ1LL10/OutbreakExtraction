using Outbreak.Core;
using Outbreak.Interaction;
using UnityEngine;

namespace Outbreak.Raids
{
    // Authored schematic: no scene rendering, enemy tracking or additional camera cost.
    public sealed class RaidMinimap : MonoBehaviour
    {
        private bool visible = true;
        private PlayerInteractor interactor;
        private RaidRuntimeContext context;
        private void Awake() { interactor = GetComponent<PlayerInteractor>(); context = FindFirstObjectByType<RaidRuntimeContext>(); }
        private void Update() { if (Input.GetKeyDown(KeyCode.M)) visible = !visible; }
        private void OnGUI()
        {
            if (!visible || context?.Flow?.State != GameState.Raid || interactor?.ActivePanel != null || Cursor.visible) return;
            using (new TacticalUI.Scope()) Draw(new Rect(22, 22, 190, 285), transform.position, transform.eulerAngles.y);
        }
        public static Vector2 MapPoint(Rect r, Vector3 p) => new Vector2(r.x + (p.x + 27) / 54 * r.width, r.yMax - (p.z + 23) / 120 * r.height);
        private static void Fill(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        public static void Draw(Rect rect, Vector3? player = null, float yaw = 0)
        {
            GUI.Box(rect, "");
            GUI.Label(new Rect(rect.x + 8, rect.y + 4, rect.width - 16, 25), "OUTSKIRTS  /  N ↑");
            var map = new Rect(rect.x + 12, rect.y + 36, rect.width - 24, rect.height - 62);
            Fill(map, new Color(.09f, .12f, .13f));
            Area(map, new Vector3(0,0,37), 10, 116, new Color(.24f,.28f,.28f));
            foreach (var p in new[] { new Vector3(-11,0,8), new Vector3(11,0,24), new Vector3(-13,0,53), new Vector3(13,0,70) })
                Area(map, p, 10, 8, new Color(.43f,.44f,.38f));
            Area(map, new Vector3(-22,0,48), 2, 70, new Color(.25f,.30f,.28f));
            var exit = MapPoint(map, new Vector3(0,0,91));
            Fill(new Rect(exit.x-5,exit.y-4,10,8), new Color(.35f,.9f,.5f));
            GUI.Label(new Rect(exit.x+7,exit.y-13,65,24), "EXIT");
            if (player.HasValue)
            {
                var p = MapPoint(map, player.Value); var matrix = GUI.matrix;
                GUIUtility.RotateAroundPivot(yaw, p);
                GUI.Label(new Rect(p.x-9,p.y-12,22,25), "▲"); GUI.matrix = matrix;
            }
            GUI.Label(new Rect(rect.x+10,rect.yMax-25,rect.width-20,24), player.HasValue ? "M · MAP ON/OFF" : "SAFEHOUSE → CHECKPOINT");
        }
        private static void Area(Rect map, Vector3 p, float width, float length, Color c)
        {
            var center = MapPoint(map,p); var size = new Vector2(width/54*map.width,length/120*map.height);
            Fill(new Rect(center-size*.5f,size),c);
        }
    }
}
