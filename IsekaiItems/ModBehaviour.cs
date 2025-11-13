using System;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DisplayItemValue
{

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        TextMeshProUGUI _text = null;
        TextMeshProUGUI Text
        {
            get
            {
                if (_text == null)
                {
                    _text = Instantiate(GameplayDataSettings.UIStyle.TemplateTextUGUI);
                }
                return _text;
            }
        }

        private float _displayTimeRemaining = 0f;
        private const float DISPLAY_DURATION = 10f;

        void Awake()
        {
            Debug.Log("DisplayItemValue Loaded!!!");
        }
        void OnDestroy()
        {
            if (_text != null)
                Destroy(_text);
        }
        void OnEnable()
        {
            ItemHoveringUI.onSetupItem += OnSetupItemHoveringUI;
            ItemHoveringUI.onSetupMeta += OnSetupMeta;
        }
        void OnDisable()
        {
            ItemHoveringUI.onSetupItem -= OnSetupItemHoveringUI;
            ItemHoveringUI.onSetupMeta -= OnSetupMeta;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                ShowSystemTime();
            }

            if (_displayTimeRemaining > 0)
            {
                _displayTimeRemaining -= Time.deltaTime;
                if (_displayTimeRemaining <= 0)
                {
                    Text.gameObject.SetActive(false);
                }
            }
        }

        private void ShowSystemTime()
        {
            Text.gameObject.SetActive(true);
            Text.transform.SetParent(Canvas.FindObjectOfType<Canvas>().transform);
            Text.transform.localScale = Vector3.one;
            Text.text = DateTime.Now.ToString("HH:mm:ss");
            Text.fontSize = 36f;
            Text.alignment = TextAlignmentOptions.Center;
            _displayTimeRemaining = DISPLAY_DURATION;
        }

        private void OnSetupMeta(ItemHoveringUI uI, ItemMetaData data)
        {
            Text.gameObject.SetActive(false);
        }

        private void OnSetupItemHoveringUI(ItemHoveringUI uiInstance, Item item)
        {
            if (item == null)
            {
                Text.gameObject.SetActive(false);
                return;
            }

            Text.gameObject.SetActive(true);
            Text.transform.SetParent(uiInstance.LayoutParent);
            Text.transform.localScale = Vector3.one;
            Text.text = $"${item.GetTotalRawValue() / 2}";
            Text.fontSize = 20f;
        }
    }
}