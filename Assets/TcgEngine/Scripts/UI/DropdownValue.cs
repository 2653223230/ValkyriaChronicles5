using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// Class that let you store a ID with each element of a Dropdown
    /// </summary>

    [System.Serializable]
    public class DropdownValueItem
    {
        public string id;
        public string text;
    }

    [RequireComponent(typeof(Dropdown))]
    public class DropdownValue : MonoBehaviour
    {
        public UnityAction<int, string> onValueChanged;

        private List<DropdownValueItem> values = new List<DropdownValueItem>();

        private Dropdown dropdown;

        void Awake()
        {
            dropdown = GetComponent<Dropdown>();
            if (dropdown != null)
                dropdown.onValueChanged.AddListener(OnChangeValue);
        }

        private void Start()
        {

        }

        public void AddOption(string id, string text)
        {
            if (dropdown == null)
                dropdown = GetComponent<Dropdown>();
            if (dropdown == null)
                return;

            Dropdown.OptionData option = new Dropdown.OptionData(text);
            dropdown.options.Add(option);
            DropdownValueItem item = new DropdownValueItem();
            item.id = id;
            item.text = text;
            values.Add(item);
            dropdown.RefreshShownValue();
        }

        public void ClearOptions()
        {
            values.Clear();
            if (dropdown == null)
                dropdown = GetComponent<Dropdown>();
            if (dropdown == null)
                return;
            dropdown.ClearOptions();
        }

        public void SetValue(string value)
        {
            if (dropdown == null)
                dropdown = GetComponent<Dropdown>();
            if (dropdown == null)
                return;

            int index = 0;
            foreach (DropdownValueItem item in values)
            {
                if (item.id == value)
                    dropdown.value = index;
                index++;
            }
        }

        private void OnChangeValue(int selected_index)
        {
            if (dropdown == null)
                return;
            if (selected_index >= 0 && selected_index < values.Count)
            {
                DropdownValueItem value = values[selected_index];
                if (onValueChanged != null)
                    onValueChanged.Invoke(selected_index, value.id);
            }
        }

        public DropdownValueItem GetSelected()
        {
            if (dropdown == null)
                dropdown = GetComponent<Dropdown>();
            if (dropdown == null)
                return null;
            if (dropdown.value >= 0 && dropdown.value < values.Count)
            {
                DropdownValueItem item = values[dropdown.value];
                return item;
            }
            return null;
        }

        public string GetSelectedValue()
        {
            DropdownValueItem item = GetSelected();
            if (item != null)
                return item.id;
            return "";
        }

        public string GetSelectedText()
        {
            DropdownValueItem item = GetSelected();
            if (item != null)
                return item.text;
            return "";
        }

        public bool interactable
        {
            get { return dropdown != null && dropdown.interactable; }
            set { if (dropdown != null) dropdown.interactable = value; }
        }

        public int value
        {
            get { return dropdown != null ? dropdown.value : 0; }
            set
            {
                if (dropdown == null)
                    return;
                dropdown.value = value;
                dropdown.RefreshShownValue();
            }
        }
    }
}