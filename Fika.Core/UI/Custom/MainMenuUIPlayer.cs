using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Fika.Core.UI.FikaUIGlobals;

public class MainMenuUIPlayer : MonoBehaviour
{
	[SerializeField]
	public TextMeshProUGUI PlayerName;
	[SerializeField]
	public TextMeshProUGUI PlayerLevel;
	[SerializeField]
	public TextMeshProUGUI PlayerStatus;
	[SerializeField]
	public Image StatusImage;

	public void SetActivity(string nickname, int level, EFikaPlayerPresence presence)
	{
		PlayerName.text = nickname;
		PlayerLevel.text = $"({level})";
		string status = presence switch
		{
			EFikaPlayerPresence.IN_MENU => "主界面",
			EFikaPlayerPresence.IN_RAID => "战局中",
			EFikaPlayerPresence.IN_STASH => "仓库",
			EFikaPlayerPresence.IN_HIDEOUT => "藏身处",
			EFikaPlayerPresence.IN_FLEA => "跳蚤市场",
			_ => "主界面",
		};
		PlayerStatus.text = status;
		SetImageColor(presence);
	}

	private void SetImageColor(EFikaPlayerPresence presence)
	{
		StatusImage.color = presence switch
		{
			EFikaPlayerPresence.IN_MENU => Color.green,
			EFikaPlayerPresence.IN_RAID => Color.red,
			EFikaPlayerPresence.IN_STASH or EFikaPlayerPresence.IN_HIDEOUT or EFikaPlayerPresence.IN_FLEA => Color.yellow,
			_ => Color.green,
		};
	}
}
