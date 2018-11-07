using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class HorizontalSlidableSelector : MonoBehaviour, IDragHandler, IEndDragHandler, IBeginDragHandler
{
    #region subclass
    public class ItemSelectEvent : UnityEvent<GameObject, object>
    {

    }

    public class DataSource<T> : IDataSource where T : class
    {
        readonly T[] datas;

        public object this[int index]
        {
            get { return datas[index]; }
        }

        public int Count
        {
            get { return datas.Length; }
        }

        public DataSource(IEnumerable<T> datas)
        {
            this.datas = datas.ToArray();
        }

        public void ProvideData(GameObject gameObject, int index)
        {
            gameObject.GetComponent<IUpdateItemData<T>>().UpdateItemData(index, gameObject, datas[index]);
        }
    }

    public interface IDataSource
    {
        object this[int index] { get; }
        int Count { get; }
        void ProvideData(GameObject gameObject, int index);
    }

    public interface IUpdateItemData<T> where T : class
    {
        void UpdateItemData(int index, GameObject gameObject, T data);
    }
    #endregion

    #region events

    public ItemSelectEvent onItemSelected = new ItemSelectEvent();

    private Action<bool> onActionMove = null;

    #endregion

    [SerializeField]
    private RectTransform itemTemplate;

    [SerializeField]
    private Vector2 itemSize = new Vector2(300, 500);

    [SerializeField]
    private int padding = 50;

    [SerializeField]
    private int selectedIndex = 0;

    [SerializeField]
    private float selectedScale = 1.2f;

    [SerializeField]
    private GameObject maskObject = null;

    [SerializeField]
    private float pageRotateScale = 80;

    [SerializeField]
    private bool isCaculateScreenWidthPageRotate = false;

    private Vector3 originalItemScale = Vector3.one;
    private Vector3 originalItemRotate = Vector3.zero;
    private Vector2 originalItemAnchorPosition = Vector2.zero;
    private bool isInitOrigialItem = false;

    private List<float> depthItemZ = new List<float>();

    private IDataSource dataSource = null;
    private List<RectTransform> items;

    public int ItemCount
    {
        get
        {
            if (dataSource == null)
            {
                return 0;
            }
            return dataSource.Count;
        }
    }
    public int SelectedIndex
    {
        get { return selectedIndex; }
    }

    private void Awake()
    {
        CheckAndInitOriginalItem();
    }

    private void CheckAndInitOriginalItem()
    {
        if (!isInitOrigialItem)
        {
            isInitOrigialItem = true;
            originalItemScale = itemTemplate.transform.localScale;
            originalItemRotate = itemTemplate.transform.localEulerAngles;
            originalItemAnchorPosition = itemTemplate.anchoredPosition;
        }
    }

    private void Init()
    {
        CheckAndInitOriginalItem();
        if (maskObject == null)
        {
            throw new Exception("you have to set the maskObject field.");
        }

        itemTemplate.transform.localScale = originalItemScale;
        itemTemplate.transform.localEulerAngles = originalItemRotate;
        itemTemplate.anchoredPosition = originalItemAnchorPosition;
        if (tween != null && tween.IsActive() && tween.IsPlaying())
        {
            tween.Kill(false);
        }
        if (items != null)
        {
            for (int i = items.Count - 1; i >= 0; --i)
            {
                if (items[i] != itemTemplate && items[i] != null)
                    GameObject.DestroyImmediate(items[i].gameObject);
            }
            items.Clear();
        }

        items = new List<RectTransform>();
        items.Add(itemTemplate);
        for (int i = 1; i < ItemCount; i++)
        {
            var item = Instantiate(itemTemplate);
            item.name = itemTemplate.name + "_" + i;
            items.Add(item);
        }

        var anchorPos = itemTemplate.anchoredPosition;
        for (int i = 0; i < ItemCount; i++)
        {
            var item = items[i];

            item.SetParent(maskObject.transform, false);

            var p = item.anchoredPosition;
            
            if (i < selectedIndex)
            {
                p.x -= item.sizeDelta.x / 2 + (selectedIndex - i) * padding + (selectedIndex - i - 1) * item.sizeDelta.x + item.sizeDelta.x / 2;
            }
            else if (i > selectedIndex)
            {
                p.x += item.sizeDelta.x / 2 + (i - selectedIndex) * padding + (i - selectedIndex - 1) * item.sizeDelta.x + item.sizeDelta.x / 2;
            }

            item.anchoredPosition = p;
        }

        UpdateItemRotate();
        UpdateItemScale();
        UpdateItemSiblingIndex();
    }

    public void SetMoveAction(Action<bool> action)
    {
        onActionMove = action;
    }

    public void SetData<T>(IEnumerable<T> datas) where T : class
    {
        if (datas == null)
            return;

        dataSource = new DataSource<T>(datas);

        Init();

        var count = Mathf.Min(ItemCount, items.Count);
        for (int i = 0; i < count; i++)
        {
            dataSource.ProvideData(items[i].gameObject, i);
        }

        onItemSelected.Invoke(items[selectedIndex].gameObject, dataSource[selectedIndex]);

        if (onActionMove != null)
            onActionMove.Invoke(false);
    }

    private Tweener tween = null;
    public void SelectItem(GameObject gameObject)
    {
        var rect = gameObject.GetComponent<RectTransform>();

        var delta = 0 - rect.anchoredPosition.x;
        if (onActionMove != null)
        {
            onActionMove.Invoke(true);
        }

        _allowDraging = true;


    }

    public void SelectIndex(int index)
    {
        if (index < 0 || index > ItemCount - 1)
        {
            return;
        }

        selectedIndex = index;

        ScrollToIndex(selectedIndex);
    }

    private void SmoothDrag(float cur, float end, float dur, Action funcUpdate, Action funcComplete)
    {
        if (tween != null && tween.IsActive() && tween.IsPlaying())
        {
            tween.Kill(true);
        }
        float tweenValue = 0;
        float lastValue = 0;
        var dragHandler = this as IDragHandler;
        tween = DOTween.To(x => tweenValue = x, cur, end, dur).SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                float offset = tweenValue - lastValue;
                OnDrag(new PointerEventData(EventSystem.current) { delta = new Vector2(offset, 0) });
                lastValue = tweenValue;
                if (funcUpdate != null)
                {
                    funcUpdate.Invoke();
                }

            })
            .OnComplete(() =>
            {
                float offset = tweenValue - lastValue;
                OnDrag(new PointerEventData(EventSystem.current) { delta = new Vector2(offset, 0) });
                if (funcComplete != null)
                {
                    funcComplete.Invoke();
                }
            });
    }

    private bool _allowDraging = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (SelectedIndex == 0 && eventData.delta.x > 0f)
        {
            return;
        }

        if (SelectedIndex == ItemCount - 1 && eventData.delta.x < 0f)
        {
            return;
        }

        _allowDraging = true;

        if (onActionMove != null)
            onActionMove.Invoke(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_allowDraging)
        {
            return;
        }

        eventData.Use();

        if (items[0].anchoredPosition.x > 0f)
        {
            eventData.useDragThreshold = true;
            //delta = -items[0].anchoredPosition.x;
        }
        else if (items[ItemCount - 1].anchoredPosition.x < 0f)
        {
            eventData.useDragThreshold = true;
            //delta = -items[itemCount - 1].anchoredPosition.x;
        }

        var delta = eventData.delta.x;

        for (int i = 0; i < ItemCount; i++)
        {
            var item = items[i];

            var p = item.anchoredPosition;

            p.x += delta;

            item.anchoredPosition = p;
        }

        UpdateItemScale();
        UpdateItemRotate();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        var clampIndex = 0;

        if (items[0].anchoredPosition.x >= 0f)
        {
            clampIndex = 0;
        }
        else if (items[ItemCount - 1].anchoredPosition.x <= 0f)
        {
            clampIndex = ItemCount - 1;
        }
        else
        {
            for (int i = 1; i < ItemCount; i++)
            {
                if (Mathf.Abs(items[i].anchoredPosition.x - 0f) < Mathf.Abs(items[clampIndex].anchoredPosition.x - 0f))
                {
                    clampIndex = i;
                }
            }
        }

        SelectIndex(clampIndex);
    }

    private void ScrollToIndex(int index)
    {
        _allowDraging = true;

        var selectedItem = items[selectedIndex];
        var data = dataSource[selectedIndex];
        var delta = 0 - selectedItem.anchoredPosition.x;

        SmoothDrag(0, delta, Mathf.Abs(delta) * 0.2f / Screen.width, null, () =>
        {
            if (onActionMove != null)
                onActionMove.Invoke(false);


            onItemSelected.Invoke(selectedItem.gameObject, data);

            Debug.Log(selectedIndex);

            UpdateItemScale();
            UpdateItemRotate();

            _allowDraging = false;
        });
    }

    private void UpdateItemScale()
    {
        for (int i = 0; i < ItemCount; i++)
        {
            var item = items[i];
            var rect = item.GetComponent<RectTransform>();

            var offset = Mathf.Abs(rect.anchoredPosition.x);
            var maxOffset = itemSize.x / 2 + itemSize.x * selectedScale / 2 + padding;
            offset = offset > maxOffset ? maxOffset : offset;

            var t = offset / maxOffset;
            var scale = Mathf.Lerp(selectedScale, originalItemScale.y, t);
            rect.localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void UpdateItemSiblingIndex()
    {
        for (int i = 0; i < ItemCount; ++i)
        {
            var item = items[i];
            var rect = item.GetComponent<RectTransform>();
            int siblingIndex = (i < selectedIndex ? i : (ItemCount - 1 + (selectedIndex - i)));
            rect.SetSiblingIndex(siblingIndex);
        }
    }

    private void UpdateItemRotate()
    {
        int index = selectedIndex - 1;
        float pageRotateRate = pageRotateScale / (isCaculateScreenWidthPageRotate ? Screen.width : 1080);
        for (int i = 0; i < ItemCount; i++)
        {
            var item = items[i];
            var rect = item.GetComponent<RectTransform>();

            var posX = rect.anchoredPosition.x;
            var eulerAnglers = rect.localRotation.eulerAngles;
            eulerAnglers.y = posX * pageRotateRate;
            rect.eulerAngles = eulerAnglers;
        }

        if (null == depthItemZ || depthItemZ.Count < ItemCount)
        {
            depthItemZ = new List<float>();
            float z = itemTemplate.anchoredPosition3D.z;
            for (int i = 0; i < ItemCount; ++i)
                depthItemZ.Add(z);
        }

        // 更新深度值
        float normalOffset = itemSize.x + padding;
        float maxOffset = normalOffset;
        float depthIndex = -1;
        for (int i = 0; i < ItemCount; i++)
        {
            var item = items[i];
            var rect = item.GetComponent<RectTransform>();

            var offset = rect.anchoredPosition.x;
            float sign = offset > 0 ? 1 : -1;
            float t = 0;
            if (Mathf.Abs(offset) > maxOffset)
            {
                offset -= sign * maxOffset;
                t = Mathf.Repeat(offset / normalOffset, 1);
                depthIndex = sign * 1 + Mathf.FloorToInt(offset / normalOffset);
            }
            else
            {
                t = Mathf.Repeat(offset / maxOffset, 1);
                depthIndex = Mathf.FloorToInt(offset / maxOffset);
            }
            depthIndex += selectedIndex;

            float scale = Mathf.Lerp(depthItemZ[(int)Mathf.Clamp(depthIndex, 0, ItemCount - 1)], depthItemZ[(int)Mathf.Clamp(depthIndex + 1, 0, ItemCount - 1)], t);
            var pos = rect.anchoredPosition3D;
            pos.z = scale;
            rect.anchoredPosition3D = pos;
        }
    }


    public void Clear()
    {
        for (int i = items.Count - 1; i >= 0; --i)
        {
            var item = items[i];
            if (item != itemTemplate)
            {
                items.Remove(item);
                GameObject.Destroy(item);
            }
        }
    }
}