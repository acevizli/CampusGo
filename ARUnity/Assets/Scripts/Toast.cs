using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts
{
    class Toast: MonoBehaviour
    {

        public static Toast Instance;
        public Text textfield;
        // Start is called before the first frame update
        void Start()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        private void OnLevelWasLoaded(int level)
        {
            textfield = FindObjectOfType<Text>();
            textfield.enabled = false;
        }
        public void TextShow(string text)
        {
            textfield.text = text;
            textfield.enabled = true;
            StartCoroutine(HideText());

        }

        public IEnumerator HideText()
        {
            yield return new WaitForSeconds(2.5f);
            textfield.enabled = false;
        }
    }
}
