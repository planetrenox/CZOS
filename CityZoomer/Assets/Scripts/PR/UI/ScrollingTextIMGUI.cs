using UnityEngine;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;


namespace PR
{
    public class ScrollingTextIMGUI : MonoBehaviour
    {
        private string MarqueeMessage = "";
        private readonly float scrollSpeed = 30;
        private Rect RectMarqueeContainer, RectMarquee;
        private bool isVisibleMarquee, isInitMarquee;
        private RectTransform RectTransform_Panel_Menu;
        


        
        private async UniTaskVoid GetNewsForApp()
        {
            var txt = (await UnityWebRequest.Get("https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=1745790&count=4").SendWebRequest()).downloadHandler.text;
            var newsArray = SimpleJSON.JSON.Parse(txt)["appnews"]["newsitems"].AsArray;
            for (var i = newsArray.Count - 1; i >= 0; i--) MarqueeMessage += " " + (newsArray[i]["contents"] + " " + newsArray[i]["title"]).Replace("\\n", " ");
            isVisibleMarquee = true;
        }
        
        
        private void Start()
        {
            RectTransform_Panel_Menu = GameObject.Find("Panel_Menu").GetComponent<RectTransform>();
            GetNewsForApp().Forget();
        }

        private void OnGUI()
        {
            if (!isVisibleMarquee) return;
            var dimensions = GUI.skin.label.CalcSize(new GUIContent(MarqueeMessage));
            var rec = PR.Utility.GetScreenCoordinates(RectTransform_Panel_Menu);
            RectMarqueeContainer = new Rect(rec.x, rec.y - dimensions.y, rec.width, dimensions.y);
            // change RectMarquee.width to the width of the MarqueeMessage
            if (!isInitMarquee)
            {
                RectMarquee = new Rect(-dimensions.x, 0, dimensions.x, RectMarqueeContainer.y);
                isInitMarquee = true;
            }
            
            RectMarqueeContainer = GUI.Window(1, RectMarqueeContainer, MarqueeWindow, "", GUI.skin.box);
        }


        private void MarqueeWindow(int windowID)
        {
            RectMarquee.x += Time.deltaTime * scrollSpeed;
            // place RectMarquee inside the window
            GUI.Label(RectMarquee, MarqueeMessage);
        }
    }
}