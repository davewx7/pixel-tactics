using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopItemEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Equipment equipment = null;

    [SerializeField]
    ShopDialog _shopDialog = null;

    [SerializeField]
    Sprite _bg = null, _bgHighlight = null;

    [SerializeField]
    Image _image = null, _slotImage = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _nameText = null, _costText = null, _statusText = null;

    public string cannotInteractMessage = "";

    bool _interactable = true;
    public bool interactable {
        get {
            return _interactable;
        }
        set {
            _interactable = value;
            _image.color = _interactable ? Color.white : Color.gray;
            if(_interactable == false) {
                _image.sprite = _bg;
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(interactable) {
            _image.sprite = _bgHighlight;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _image.sprite = _bg;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(interactable == false) {
            return;
        }

        if(equipment != null) {
            _shopDialog.BuyEquipment(equipment);
        }
    }

    public int inconvoy {
        get {
            if(equipment != null) {
                return _shopDialog.HasInConvoy(equipment);
            }

            return 0;
        }
    }

    public int cost {
        get {
            if(inconvoy > 0) {
                return 0;
            }

            if(equipment != null) {
                return _shopDialog.GetPrice(equipment.price);
            }

            return 0;
        }
    }

    public int normalCost {
        get {
            if(equipment != null) {
                return equipment.price;
            }

            return 0;
        }
    }

    public void Init()
    {
        if(equipment != null) {
            _slotImage.sprite = equipment.icon;

            _slotImage.material = new Material(GameConfig.instance.GetMaterialForInventorySlot(equipment.tier));
            _slotImage.material.SetFloat("_hueshift", equipment.hueShift);

            _nameText.text = equipment.description;
            UnitStatusPanel.SetTooltip(_image, equipment.GetToolTip() + cannotInteractMessage);
        }

        if(normalCost > cost) {
            _costText.text = string.Format("<s> <b><color=#aaaaaa>{0}</color></b> </s> {1}", normalCost, cost);
        } else {
            _costText.text = string.Format("{0}", cost);
        }

        if(inconvoy > 0) {
            int n = inconvoy;
            if(equipment != null && _shopDialog.equipmentBeingBought.Contains(equipment)) {
                --n;
            }

            _statusText.text = string.Format("{0} in convoy", n);
        } else {
            _statusText.text = _shopDialog.EquipmentStatus(equipment);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
    }
}
