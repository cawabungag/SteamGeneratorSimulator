using OPCClientInterface;
using UnityEngine;
using UnityEngine.UI;

public class OPCItem : MonoBehaviour
{
	public Text IDTxt;
	public Text NameTxt;
	public Text DescriptionTxt;
	public Text TypeTxt;
	public Text ValueTxt;
	public Text AuthTxt;
	public Button BindBtn;

	public void Init(OPCNode node)
	{
		if (node == null) return;
		IDTxt.text = node.NodeId?.ToString();
		NameTxt.text = node.Name;
		ValueTxt.text = node.Value?.ToString();
		TypeTxt.text = node.Type;
		AuthTxt.text = node.Auth;
		DescriptionTxt.text = node.Description;
	}
}