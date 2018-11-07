using UnityEngine;
using UnityEngine.UI;

public class ItemUpdater : MonoBehaviour, HorizontalSlidableSelector.IUpdateItemData<TestData>
{
    [SerializeField]
    private Image image;

    [SerializeField]
    private Text nameText;


    public void UpdateItemData(int index, GameObject gameObject, TestData data)
    {
        image.color = data.Color;
        nameText.text = data.Name;
    }
}
