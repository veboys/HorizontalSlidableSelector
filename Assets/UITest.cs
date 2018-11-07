using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UITest : MonoBehaviour
{
    [SerializeField]
    private HorizontalSlidableSelector selector;
    [SerializeField]
    private Button btnLeft;
    [SerializeField]
    private Button btnRight;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(int.MaxValue);

        var datas = new List<TestData>
        {
            new TestData{Name = "test1", Color = Color.black},
            new TestData{Name = "test2", Color = Color.blue},
            new TestData{Name = "test3", Color = Color.gray},
            new TestData{Name = "test4", Color = Color.green},
            new TestData{Name = "test5", Color = Color.magenta},
            new TestData{Name = "test6", Color = Color.red},
            new TestData{Name = "test7", Color = Color.white},
            new TestData{Name = "test8", Color = Color.yellow},
            new TestData{Name = "test9", Color = new Color(0.3f,0.24f,0.78f,1f)},
            new TestData{Name = "test10", Color = new Color(0.9f,0.24f,0.78f,1f)}
        };

        selector.onItemSelected.AddListener(OnItemSelectedHandler);

        selector.SetData(datas);

        btnLeft.onClick.AddListener(BtnLeft_Clicked);
        btnRight.onClick.AddListener(BtnRight_Clicked);
    }

    private void BtnLeft_Clicked()
    {
        selector.SelectIndex(selector.SelectedIndex - 1);
    }

    private void BtnRight_Clicked()
    {
        selector.SelectIndex(selector.SelectedIndex + 1);
    }

    private void OnItemSelectedHandler(GameObject go, object info)
    {
        var data = info as TestData;

        Debug.LogFormat("{0} selected...", data.Name);
    }
   
}

public class TestData
{
    public Color Color { get; set; }
    public string Name { get; set; }
}
