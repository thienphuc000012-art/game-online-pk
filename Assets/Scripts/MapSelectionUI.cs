//using UnityEngine;
//using UnityEngine.UI;

//public class MapSelectionUI : MonoBehaviour
//{
//    public Image[] mapImages; // gán các hình map trong Inspector
//    public BasicSpawner spawner;

//    private int selectedMap = -1;

//    public void OnSelectMap(int index)
//    {
//        spawner.SetMap(index);

//        // highlight map được chọn
//        for (int i = 0; i < mapImages.Length; i++)
//        {
//            mapImages[i].color = (i == index) ? Color.green : Color.white;
//        }

//        selectedMap = index;
//    }
//}
