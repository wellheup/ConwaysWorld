using UnityEngine;
using UnityEngine.UI;

public class BaseTile : MonoBehaviour
{
	[SerializeField] private Image _image;

	public Image Image { get { return _image; } }
}