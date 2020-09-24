using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;

    void Awake() {
        if (instance == null) {
            instance = this;
        }
    }

    public static void UpdateUI() {
        instance.UpdateStats();
        instance.UpdateBoard();

        instance.UpdateQueueAttackText();

        if (instance.player != null) {
            //update all the cards in the player's hand
            for (int i = 0; i < instance.player.hand.cards.Count; i++)
            {
                Card c = instance.player.hand.cards[i];
                c.UpdateDisplay();
            }
        }
        
    }

    public TextMeshProUGUI phaseText;

    public TextMeshPro energyText, healthText, blockText, strText, agiText, staText, intText;

    public Button endTurnbutton, skipDiscoverButton;
    public GameObject discoveryTitle;

    public GameObject persistentTextObject;
    public TextMeshProUGUI persistentTextText;
    public Button persistentTextButton;

    public CardDisplay previewDisplay;

    public GameObject textPopup;

    public Transform mainTextPopupPoint, centrePoint, enemyTextPopupPoint;

    public Transform cursor;

    public GameObject choiceUI;
    public TextMeshProUGUI choice_choice1text, choice_choice2text, choice_text;
    public Button choice1Button, choice2Button;

    public TextMeshProUGUI leftClickText, rightclickText;

    [BoxGroup("Board")] public TextMeshPro drawPileText;
    [BoxGroup("Board")] public TextMeshPro discardPileText;
    [BoxGroup("Board")] public TextMeshPro libraryPileText;
    [BoxGroup("Board")] public TextMeshPro burnPileText;

    [BoxGroup("Opponent")] public TextMeshPro opponentHealth;
    [BoxGroup("Opponent")] public TextMeshPro opponentBlock;
    [BoxGroup("Opponent")] public TextMeshPro opponentEnergy;
    [BoxGroup("Opponent")] public TextMeshPro opponentStr;
    [BoxGroup("Opponent")] public TextMeshPro opponentAgi;
    [BoxGroup("Opponent")] public TextMeshPro opponentSta;
    [BoxGroup("Opponent")] public TextMeshPro opponentInt;

    public TextMeshPro queueAttackText, opponentQueueAttackText;
    public TextMeshProUGUI previewCardDescriptionText;

    Player player, opponent;
    void Start() {
        player = GameManager.instance.player;
        opponent = GameManager.instance.opponent;
        choiceUI.SetActive(false);

    }

    public void UpdatePhase() {
        phaseText.text = GameManager.instance.currentPhase.ToString();
    }

    void UpdatePreviewCard(Card card) {
        previewDisplay.gameObject.SetActive(true);

        previewDisplay.card = card;

        previewDisplay.UpdateDisplay();

        previewCardDescriptionText.text = GenerateCardDescription(card);
    }

    void HidePreview() {
        previewDisplay.gameObject.SetActive(false);
        UpdateCursorText(null);
        previewCardDescriptionText.text = "";
    }

    string GenerateCardDescription(Card c) {
        string result = "";

        

        foreach (Card.CardAttribute att in c.attributes.Distinct()) {
            result += c.GetAttributeDescription(att) + "\n";
        }
        foreach (Card.CardKeyword kw in c.keywords.Distinct())
        {
            result += c.GetKeywordDescription(kw) + "\n";
        }

        return result;
    }

    void UpdateCursorText(Card card) {
        

        if (card == null)
        {
            leftClickText.text = "";
            rightclickText.text = "";
        }
        else {
            if (card.currentPile != card.owner.hand)
            {
                leftClickText.text = "";
                rightclickText.text = "";
            }
            else {
                if (card.GetIsPlayable()) {
                    leftClickText.text = "Play";
                }
                rightclickText.text = card.GetAltText();

            }

        }
    }
   

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, 100f, (int)(1 << 8)))
        {
            //Debug.Log(hit.collider.gameObject, hit.collider.gameObject);
            if (hit.transform.GetComponentInParent<Card>() != null)
            {
                //we've hit a card
                Card selected = hit.transform.GetComponentInParent<Card>();
                //if (selected.faceUp && (selected.currentPile == selected.owner.hand || selected.currentPile == selected.owner.queue || selected.currentPile == selected.owner.viewer))
                if(selected.faceUp)
                {
                    UpdatePreviewCard(selected);
                    UpdateCursorText(selected);
                }
                else {
                    HidePreview();
                }

            }
            
        }
        else {
            HidePreview();
        }

        cursor.transform.position = Input.mousePosition;
    }

    public void UpdateStats() {
        if (player == null) return; //if we're not set up yet

        energyText.text = player.energy.ToString();
        healthText.text = player.health.ToString();
        blockText.text = player.block > 0?player.block.ToString(): "";
        strText.text = "STR:\n" + player.strength;
        agiText.text = "AGI:\n" + player.agility;
        staText.text = "STA:\n" + player.stamina;
        intText.text = "INT:\n" + player.intelligence;

        opponentEnergy.text = opponent.energy.ToString();
        opponentHealth.text = opponent.health.ToString();
        opponentBlock.text = opponent.block > 0?opponent.block.ToString():"";
        opponentStr.text = "STR:\n" + opponent.strength;
        opponentAgi.text = "AGI:\n" + opponent.agility;
        opponentSta.text = "STA:\n" + opponent.stamina;
        opponentInt.text = "INT:\n" + opponent.intelligence;
    }

    public void EndTurn() {
        GameManager.instance.FinishTurn();
    }

    public void SkipDiscover() {
        GameManager.instance.SkipDiscover();
    }

    public void PersistentButton()
    {
        GameManager.instance.player.SubmitChoice(0);
    }

    public static void Popup(string text, Color? color = null, Transform point = null, float size = 0, float direction = 30f, float variance = 0f) {
        instance.TextPopup(text, color, point, size, direction, variance);
    }

    public static void Popup(string text, Vector3 worldPoint, Color? color = null, float size = 0, float direction = 30f, float variance = 0f, float fadeSpeed = 0.5f)
    {
        instance.TextPopup(text, worldPoint, color, size, direction, variance, fadeSpeed);
    }

    public void TextPopup(string text, Color? color = null, Transform point = null, float size = 0, float direction = 30f, float variance = 0f) {
        TextPopup spawned = Instantiate(textPopup, transform).GetComponent<TextPopup>();
        spawned.transform.position = (point ?? ((GameManager.instance.activePlayer == player)?mainTextPopupPoint: enemyTextPopupPoint)).position;
        if(point != null) spawned.transform.position += new Vector3(5f, 0f, 0f);

        spawned.text = text;
        spawned.color = color ?? Color.black;

        spawned.floatSpeed = new Vector3(
            (variance == 0) ? (0f) : (Random.Range((variance - (variance * 0.5f)), (variance + (variance * 0.5f)))),
            direction,
            0
            );
        spawned.fadeSpeed = 1f;

        if (size > 0) {
            spawned._text.enableAutoSizing = false;
            spawned._text.fontSize = size;
        }

        spawned.Init();
    }

    public void TextPopup(string text, Vector3 worldPoint, Color? color = null, float size = 0, float direction = 30f, float variance = 0f, float fadeSpeed = 0.7f)
    {
        TextPopup spawned = Instantiate(textPopup, transform).GetComponent<TextPopup>();
        spawned.transform.position = Camera.main.WorldToScreenPoint(worldPoint);

        spawned.text = text;
        spawned.color = color ?? Color.black;
        spawned.floatSpeed = new Vector3(
            (variance == 0) ? (0f) : (Random.Range((variance - (variance * 0.5f)), (variance + (variance * 0.5f)))),
            direction,
            0
            );
        spawned.fadeSpeed = fadeSpeed;

        if (size > 0)
        {
            spawned._text.enableAutoSizing = false;
            spawned._text.fontSize = size;
        }

        spawned.Init();
    }

    public void UpdateBoard() {
        if (player == null) return; //if we're not setup yet

        drawPileText.text = "Draw\n" + player.drawPile.cards.Count;
        discardPileText.text = "Discard\n" + player.discardPile.cards.Count;
        libraryPileText.text = "Library\n" + player.library.cards.Count;
        burnPileText.text = "Burn\n" + player.burnPile.cards.Count;
    }

    public void StartChoice(string choice1, string choice2, string choiceText, bool valid1 = true, bool valid2 = true) {
        choice_choice1text.text = choice1;
        choice_choice2text.text = choice2;
        choice_text.text = choiceText;

        choice1Button.interactable = valid1;
        choice2Button.interactable = valid2;

        choiceUI.SetActive(true);
    }

    public void SubmitChoice(int result) {
        choiceUI.SetActive(false);
        GameManager.instance.player.SubmitChoice(result);
    }

    public int humanAttack = 0;
    public int computerAttack = 0;
    public void UpdateQueueAttackText() {
        if (player == null) return;

        //for the human
        humanAttack = 0;
        for (int i = 0; i < player.queue.cards.Count; i++) {
            Card c = player.queue.cards[i];
            if (c.currentDelay == 0) {
                for (int j = 0; j < c.keywords.Count; j++) {
                    Card.CardKeyword keyword = c.keywords[j];
                    if (keyword.keyword == Card.CardKeyword.Keyword.Attack) {
                        humanAttack += keyword.value + player.strength;
                    }
                }
            }
        }

        computerAttack = 0;
        for (int i = 0; i < opponent.queue.cards.Count; i++)
        {
            Card c = opponent.queue.cards[i];
            if (c.currentDelay == 0)
            {
                for (int j = 0; j < c.keywords.Count; j++)
                {
                    Card.CardKeyword keyword = c.keywords[j];
                    if (keyword.keyword == Card.CardKeyword.Keyword.Attack)
                    {
                        computerAttack += keyword.value + opponent.strength;
                    }
                }
            }
        }

        if (humanAttack > 0)
        {
            if (opponent.block > 0)
            {
                queueAttackText.text = "^\n" + Mathf.Max(0,(humanAttack - opponent.block)).ToString() + " <size=4>(" + humanAttack + "-" + opponent.block + ")";

            }
            else {
                queueAttackText.text = "^\n" + humanAttack.ToString();

            }
            if (humanAttack > opponent.block)
            {
                queueAttackText.color = Color.red;
            }
            else if (humanAttack == opponent.block)
            {
                queueAttackText.color = Color.blue;

            }
            else {
                queueAttackText.color = Color.yellow;

            }


        }
        else {
            queueAttackText.text = "";
        }


        if (computerAttack > 0)
        {
            if (player.block > 0)
            {
                opponentQueueAttackText.text = Mathf.Max(0, (computerAttack - player.block)).ToString() + " <size=4>(" + computerAttack + "-" + player.block + ")" + "\nV";

            }
            else {
                opponentQueueAttackText.text = computerAttack.ToString() + "\nV";

            }

            if (computerAttack > player.block)
            {
                opponentQueueAttackText.color = Color.red;
            }
            else if (computerAttack == player.block)
            {
                opponentQueueAttackText.color = Color.blue;

            }
            else
            {
                opponentQueueAttackText.color = Color.yellow;

            }
        }
        else
        {
            opponentQueueAttackText.text = "";
        }
    }

    public void ShowPersistentText(string text, string button = "") {
        if (button != "") {
            persistentTextButton.gameObject.SetActive(true);
            persistentTextButton.GetComponentInChildren<TextMeshProUGUI>().text = button;
        }
        persistentTextObject.SetActive(true);
        persistentTextText.text = text;
    }

    public void HidePersistentText() {
        persistentTextButton.gameObject.SetActive(false);
        persistentTextObject.SetActive(false);
    }

    public TMPro.TMP_InputField spawnCardinput;
    public void SpawnCardFromID() {
        int cardID = -1;
        try {
            cardID = int.Parse(spawnCardinput.text);
            CardData data = Persistence.instance.GetCardDataFromId(cardID);

            if (data != null) player.StartCoroutine(player.SpawnCard(data, player, player.hand));

        }
        catch {
            Debug.Log("spawn card failed (" + spawnCardinput.text + ")");
        }

    }
}
