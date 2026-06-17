using UnityEngine;
using UnityEngine.UI;

namespace ConwaysWorld
{
	public class BaseTile : MonoBehaviour
	{
		[SerializeField]
		private Image _image;

		[SerializeField]
		private Image[] _borders = new Image[4];

		public Image Image
		{
			get { return _image; }
		}

		public Image[] Borders
		{
			get => _borders;
			set => _borders = value;
		}
	}
}
