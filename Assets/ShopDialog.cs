using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopDialog : MonoBehaviour
{
    public Unit unit;

    UnitInfo _unitClone = null;

    [SerializeField]
    TMPro.TextMeshProUGUI _priceText = null;

    [SerializeField]
    Button _purchaseButton = null;

    [SerializeField]
    Button _storeVsPurchaseButton = null;

    List<Equipment> _startEquipment = new List<Equipment>();
    List<Equipment> _currentEquipment = new List<Equipment>();
    List<Equipment> _quaffEquipment = new List<Equipment>();

    [SerializeField]
    TMPro.TextMeshProUGUI _storeText = null, _storeButtonText = null, _purchaseButtonText = null;

    bool _storing = true;

    [SerializeField]
    ScrollRect _shopContentsScroll = null;

    [SerializeField]
    RectTransform _shopContentsPanel = null;

    [HideInInspector]
    public bool accessToConvoy = false;

    public void ToggleSellVsStore()
    {
        _storing = !_storing;

        if(_storing) {
            _storeText.text = "Store in Convoy";
            _storeButtonText.text = "Sell";
        } else {
            _storeText.text = "Sell";
            _storeButtonText.text = "Store";
        }

        Populate();
    }

    public int priceMultiplier = 100;

    public int GetPrice(int basePrice)
    {
        int result = (basePrice*priceMultiplier)/100;
        if(basePrice >= 1 && result < 1) {
            result = 1;
        }

        return result;
    }

    public string EquipmentStatus(Equipment equipment)
    {
        var market = unit.teamInfo.GetTemporaryMarket(unit.loc);
        if(market != null && market.equipment.Contains(equipment)) {
            return market.info;
        }

        return "";
    }

    public int HasInConvoy(Equipment equipment)
    {
        int result = 0;
        foreach(var equip in unit.teamInfo.equipmentStored) {
            if(equip == equipment) {
                ++result;
            }
        }

        return result;
    }

    public bool isBuyingItems {
        get {
            if(_quaffEquipment.Count > 0) {
                return true;
            }

            foreach(Equipment equip in _currentEquipment) {
                if(_startEquipment.Contains(equip) == false) {
                    return true;
                }
            }

            return false;
        }
    }

    public int currentPrice {
        get {
            int result = 0;

            //purchases
            foreach(Equipment equip in equipmentBeingBought) {
                if(unit.teamInfo.equipmentStored.Contains(equip) == false) {
                    result += GetPrice(equip.price);
                }
            }

            //sales
            if(_storing == false) {
                foreach(Equipment equip in equipmentBeingSold) {
                    result -= equip.price/2 + equip.price%2;
                }
            }

            return result;
        }
    }

    public List<Equipment> equipmentBeingBought {
        get {
            List<Equipment> result = new List<Equipment>();
            foreach(Equipment equip in _currentEquipment) {
                if(_startEquipment.Contains(equip) == false) {
                    result.Add(equip);
                }
            }
            foreach(Equipment equip in _quaffEquipment) {
                result.Add(equip);
            }
            return result;
        }
    }

    List<Equipment> equipmentBeingSold {
        get {
            List<Equipment> result = new List<Equipment>();
            foreach(Equipment equip in _startEquipment) {
                if(_currentEquipment.Contains(equip) == false) {
                    result.Add(equip);
                }
            }
            return result;
        }
    }
    
    public List<Equipment> equipment = new List<Equipment>();

    [SerializeField]
    ShopItemEntry _entryProto = null;

    List<ShopItemEntry> _items = new List<ShopItemEntry>();

    [SerializeField]
    Transform _panel = null;

    [SerializeField]
    List<ShopUnitInventorySlot> _unitSlots = new List<ShopUnitInventorySlot>();

    [SerializeField]
    List<ShopUnitInventorySlot> _saleSlots = new List<ShopUnitInventorySlot>();

    [SerializeField]
    Transform _saleSlotsTransform = null;

    [SerializeField]
    List<ShopUnitInventorySlot> _quaffSlots = new List<ShopUnitInventorySlot>();

    [SerializeField]
    Transform _quaffSlotsTransform = null;

    Color _textColor;
    
    private void OnEnable()
    {
        ++GameConfig.modalDialog;
    }

    private void OnDisable()
    {
        --GameConfig.modalDialog;
    }

    // Start is called before the first frame update
    void Start()
    {
        _textColor = _priceText.color;
        _unitClone = unit.unitInfo.Clone();

        foreach(var equip in unit.unitInfo.equipment) {
            _startEquipment.Add(equip);
            _currentEquipment.Add(equip);
        }

        int ncol = 0;
        float xpos = 0f, ypos = 0f;
        foreach(Equipment equip in equipment) {
            ShopItemEntry entry = Instantiate(_entryProto, _shopContentsPanel);
            entry.GetComponent<RectTransform>().anchoredPosition += new Vector2(xpos, ypos);
            entry.equipment = equip;
            entry.gameObject.SetActive(true);

            _items.Add(entry);

            xpos += 240f;
            ++ncol;

            if(ncol%3 == 0) {
                xpos = 0f;
                ypos -= 80f;
            }
        }

        _shopContentsPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, -ypos + 80f);
        
        Populate();

        if(accessToConvoy == false) {
            ToggleSellVsStore();
            _storeVsPurchaseButton.gameObject.SetActive(false);
        }
    }

    void Populate()
    {
        foreach(var slot in _unitSlots) {
            slot.Clear();
        }
        
        foreach(var slot in _saleSlots) {
            slot.Clear();
        }

        unit.unitInfo.equipment.Clear();

        int slotIndex = 0;
        foreach(Equipment equip in _currentEquipment) {
            unit.unitInfo.equipment.Add(equip);

            if(slotIndex < _unitSlots.Count) {
                var slot = _unitSlots[slotIndex];
                slot.SetEquipment(equip);
                ++slotIndex;
            }
        }

        slotIndex = 0;
        foreach(Equipment equip in _quaffEquipment) {
            if(slotIndex < _quaffSlots.Count) {
                var slot = _quaffSlots[slotIndex];
                slot.SetEquipment(equip);
                ++slotIndex;
            }
        }

        slotIndex = 0;
        foreach(Equipment equip in _startEquipment) {
            if(_currentEquipment.Contains(equip) == false && slotIndex < _saleSlots.Count) {
                var slot = _saleSlots[slotIndex];
                slot.SetEquipment(equip);
                ++slotIndex;
            }
        }

        if(_quaffEquipment.Count > 0) {
            _quaffSlotsTransform.gameObject.SetActive(true);
            _saleSlotsTransform.gameObject.SetActive(false);
        } else {
            _quaffSlotsTransform.gameObject.SetActive(false);
            _saleSlotsTransform.gameObject.SetActive(true);
        }

        int price = currentPrice;
        bool canAfford = price <= unit.teamInfo.gold;
        _priceText.text = price.ToString();
        _priceText.color = canAfford ? _textColor : Color.red;


        GameController.instance.unitDisplayed = unit;
        GameController.instance.RefreshUnitDisplayed(unit, _unitClone);

        bool showPurchase = (_currentEquipment.Count != _startEquipment.Count || _quaffEquipment.Count > 0);
        if(showPurchase == false) {
            for(int i = 0; i != _currentEquipment.Count; ++i) {
                if(_currentEquipment[i] != _startEquipment[i]) {
                    showPurchase = true;
                    break;
                }
            }
        }

        if(canAfford == false) {
            showPurchase = false;
        }

        _purchaseButton.gameObject.SetActive(showPurchase);

        _purchaseButtonText.text = isBuyingItems ? "Purchase" : (_storing ? "Store" : "Sell");

        foreach(var item in _items) {
            if(item.equipment != null) {
                string reason = null;
                if(CanEquip(item.equipment, out reason)) {
                    item.interactable = true;
                    item.cannotInteractMessage = "";
                } else if(CanQuaff(item.equipment)) {
                    item.interactable = true;
                    item.cannotInteractMessage = "\n<color=#ff6666>Cannot carry any more, but this\nitem may be bought and consumed immediately.</color>";
                } else {
                    item.interactable = false;
                    item.cannotInteractMessage = string.Format("\n<color=#ff6666>{0}</color>", reason);
                }
            }

            item.Init();
        }
    }

    public bool CanQuaff(Equipment equip)
    {
        return equip.canConsumeImmediately && unit.unitInfo.tired == false && _quaffEquipment.Count < 1;
    }

    public bool CanEquip(Equipment equip, out string reason)
    {
        return equip.EquippableForUnit(unit.unitInfo, out reason);
    }

    public void BuyEquipment(Equipment equip)
    {
        string reason;
        if(CanEquip(equip, out reason) == false && CanQuaff(equip)) {
            _quaffEquipment.Add(equip);
            Populate();
            return;
        }

        if(_currentEquipment.Contains(equip) == false) {
            _currentEquipment.Add(equip);
            Populate();
        }
    }

    public void SellEquipment(Equipment equip)
    {
        if(_quaffEquipment.Contains(equip)) {
            _quaffEquipment.Remove(equip);
            Populate();
        }
        else if(_currentEquipment.Contains(equip) && equip.cursed == false) {
            _currentEquipment.Remove(equip);
            Populate();
        }
    }

    public void ConfirmPurchase()
    {
        PurchaseInfo info = new PurchaseInfo() {
            unitGuid = unit.unitInfo.guid,
            cost = currentPrice,
            equipment = _currentEquipment,
            quaffEquipment = _quaffEquipment,
        };

        foreach(Equipment equip in equipmentBeingBought) {
            if(unit.teamInfo.equipmentStored.Contains(equip)) {
                info.removeEquipment.Add(equip);
            }
        }

        if(_storing) {
            info.storeEquipment = equipmentBeingSold;
        }

        GameController.instance.QueuePurchaseCommand(info);
        GameController.instance.CloseShop();
    }

    public void Cancel()
    {
        unit.unitInfo.equipment = _startEquipment;
        GameController.instance.RefreshUnitDisplayed(unit);
        GameController.instance.CloseShop();
    }

    bool _setPos = false;

    // Update is called once per frame
    void Update()
    {
        if(_setPos == false) {
            _shopContentsScroll.verticalNormalizedPosition = 1f;
            _setPos = true;
        }
    }
}
