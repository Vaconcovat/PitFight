using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Text;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    void Awake() {
        if (instance == null) {
            instance = this;
        }

        //test encode
        //string testEncodeString = JsonUtility.ToJson(playerDeck);

        //byte[] bytesToencode = Encoding.UTF8.GetBytes(testEncodeString);
        //string encoded = System.Convert.ToBase64String(bytesToencode);
        //byte[] decodedBytes = System.Convert.FromBase64String(encoded);
        //string decoded = Encoding.UTF8.GetString(decodedBytes);

        //Debug.Log(testEncodeString + " Encode: " + encoded + " Decode: " + decoded);
    }

    public WaitForSeconds shortWait = new WaitForSeconds(0.1f);
    public WaitForSeconds medWait = new WaitForSeconds(0.5f);
    public WaitForSeconds longWait = new WaitForSeconds(1f);
    public WaitForSeconds fullWait = new WaitForSeconds(2.5f);

    public enum GamePhase {Setup, Start, Queue, Play, Reset, Discover, End };

    public GamePhase currentPhase;

    public Player player, opponent;
    public Persistence.DeckListString playerDeck, opponentDeck;
    public bool useDecks = true;
    public bool skipPreDiscovery = false;
    public bool skipMulligan = false;


    public Player activePlayer;

    public GameObject cardPrefab;

    public CardData stunnedCard, fireCard, curseCard;

    public Pile playPile;

    public float gameSpeed = 1f;
    public float AISpeedMultiplier = 1.5f;

    public static float GetWaitMultiplier() {
        return (instance.gameSpeed * (instance.activePlayer == instance.opponent ? instance.AISpeedMultiplier : 1));
    }

    void Start() {

        player.character = Persistence.instance.playerCharacter;
        opponent.character = Persistence.instance.opponentCharacter;

        useDecks = !Persistence.instance.randomGeneratedDeck;
        skipPreDiscovery = Persistence.instance.skipPreDiscover;
        skipMulligan = Persistence.instance.skipMulligan;

        playerDeck = Persistence.instance.playerDeck;
        opponentDeck = Persistence.instance.opponentDeck;

        Setup();
        
    }

    Card.CardType GenerateRandomType() {
        float roll = Random.value;
        if (roll > 0.95f)
        { //5%
            return Card.CardType.Sudden;
        }
        else if (roll > 0.5f)
        {//45%
            return Card.CardType.Slow;

        }
        else if (roll > 0.1f)
        {//40%
            return Card.CardType.Fast;

        }
        else {//10%
            return Card.CardType.Slow;

        }
    }

    Card.DiscardType GenerateRandomDiscard()
    {
        float roll = Random.value;
        if (roll > 0.9f)
        { //10%
            return Card.DiscardType.Retain;
        }
        else if (roll > 0.5f)
        {//40%
            return Card.DiscardType.Discard;


        }
        else if (roll > 0.1f)
        {//40%
            return Card.DiscardType.Burn;


        }
        else
        {//10%
            return Card.DiscardType.Persistent;

        }
    }

    public void GenerateKeywords(Card card) {
        float roll = Random.value;

        Card.CardKeyword _keyword = new Card.CardKeyword();
        _keyword.keyword = (Card.CardKeyword.Keyword)Random.Range((int)0, (int)3);
        _keyword.value = Random.Range((int)1, (int)5);
        card.keywords.Add(_keyword);

        //60% chance to end generation
        while (roll > 0.6f && card.keywords.Count < 2) {
            //50% chance to be the first 3 keywords
            if (roll > 0.85f)
            {
                Card.CardKeyword keyword = new Card.CardKeyword();
                keyword.keyword = (Card.CardKeyword.Keyword)Random.Range((int)0, (int)3);
                keyword.value = Random.Range((int)1, (int)5);
                card.keywords.Add(keyword);
            }
            else {
                //the remaining keywords
                Card.CardKeyword keyword = new Card.CardKeyword();
                keyword.keyword = (Card.CardKeyword.Keyword)Random.Range((int)3, System.Enum.GetNames(typeof(Card.CardKeyword.Keyword)).Length - 3);
                keyword.value = Random.Range((int)-1, (int)3);
                card.keywords.Add(keyword);
            }
            roll = Random.value;
        }
    }

    public void GenerateAttributes(Card card)
    {
        float roll = Random.value;

        

        //70% chance to end generation
        while (roll > 0.7f && card.attributes.Count < 1)
        {
            //50% chance to be the first 2 keywords
            if (roll > 0.85f)
            {
                Card.CardAttribute keyword = new Card.CardAttribute();
                keyword.attribute = (Card.CardAttribute.Attribute)Random.Range((int)0, (int)2);
                keyword.value = 0;
                card.attributes.Add(keyword);
            }
            else
            {
                //the remaining keywords
                Card.CardAttribute keyword = new Card.CardAttribute();
                keyword.attribute = Card.CardAttribute.Attribute.Focus;//(Card.CardAttribute.Attribute)Random.Range((int)3, System.Enum.GetNames(typeof(Card.CardAttribute.Attribute)).Length - 3);
                keyword.value = Random.Range((int)1, (int)2);
                card.attributes.Add(keyword);
            }
            roll = Random.value;
        }
    }

    public List<CardData> startingCards;
    //public List<CardData> library;

    public bool playerFirst = false;
    public void Setup() {
        //spawn in a bunch of cards
        ChangePhase(GamePhase.Setup);

        activePlayer = player;

        SetupPlayer(player);
        SetupPlayer(opponent);

        UIManager.UpdateUI();
        StartCoroutine(SetupCoroutine());

        
    }

    public IEnumerator SetupCoroutine() {
        yield return instance.longWait;

        opponent.StartDraw(opponent.intelligence);
        if (!skipPreDiscovery) {
            UIManager.Popup("Pre-Discovery", Color.black, UIManager.instance.centrePoint, 100f, 0f);

            //pre-discovery
            yield return player.StartCoroutine(player.StartDiscover());
            //yield return instance.shortWait;
            //do it 3 times
            yield return player.StartCoroutine(player.StartDiscover());
            //yield return instance.shortWait;

            yield return player.StartCoroutine(player.StartDiscover());
            //yield return instance.shortWait;

            player.discardPile.Transfer(player.drawPile);
            player.drawPile.UpdateCards();
            player.drawPile.Shuffle();
        }

        

        yield return player.StartCoroutine(player.StartDrawCoroutine(player.intelligence));

        if (!skipMulligan)
        {
            //ask player if they want to mulligan
            yield return player.StartCoroutine(player.StartChoice(
                            "Keep",
                            "Mulligan",
                            "Mulligan: Discard this hand and re-draw?"
                            ));

            int result = player.GetChoiceResult();
            if (result == 0)
            {
                //cancelled
                Log.Write("Keep");

            }
            else if (result == 1)
            {
                Log.Write("Keep");

                //Nothing to do?
            }
            else if (result == 2)
            {
                Log.Write("Mulligan");

                UIManager.Popup("Mulligan");

                yield return player.StartCoroutine(player.ResetCoroutine());


            } 
        }

        activePlayer = opponent;
        ChangePhase(GamePhase.Start);

    }

    public Card SpawnCard(CardData data, Player owner) {
        Card spawned = Instantiate(cardPrefab).GetComponent<Card>();
        spawned.owner = owner;

        spawned.InitFromData(data);

        spawned.gameObject.name = spawned.cardName;
        return spawned;
    }

    void SetupPlayer(Player _player) {
        _player.health = Persistence.instance.startingHealthOverride;

        
        foreach (CardData cd in _player.character.startingCards)
        {
            Card spawned = SpawnCard(cd, _player);

            _player.drawPile.cards.Add(spawned);
        }



        //generate some cards
        if (useDecks)
        {
            foreach (int cd in (_player.human?playerDeck:opponentDeck).cards)
            {
                Card spawned = SpawnCard(Persistence.instance.GetCardDataFromId(cd), _player);

                _player.library.cards.Add(spawned);
            }
        }
        else {
            for (int i = 10; i < 30; ++i)
            {
                Card spawned = Instantiate(cardPrefab).GetComponent<Card>();

                spawned.owner = _player;
                spawned.currentPile = _player.library;
                spawned.cardName = "Generated Card" + i.ToString();
                spawned.cardType = GenerateRandomType();
                spawned.discardType = GenerateRandomDiscard();
                GenerateKeywords(spawned);
                if (spawned.cardType != Card.CardType.Sudden) GenerateAttributes(spawned);
                spawned.cardText = spawned.GenerateCardText();
                spawned.energyCost = Random.Range((int)1, (int)5);
                _player.library.cards.Add(spawned);
            }
        }
        

        _player.library.UpdateCards();
        _player.library.Shuffle();

        _player.drawPile.UpdateCards();

        _player.drawPile.Shuffle();

        UIManager.UpdateUI();
        //_player.StartDraw(5);
    }

    public void FinishTurn() {
        if (currentPhase == GamePhase.Play) {
            Log.Write(activePlayer._name + " ended their turn");
            ChangePhase(GamePhase.Reset);
        }
    }

    public void ChangePhase(GamePhase phase) {
        if (currentPhase == GamePhase.End) return;

        switch (phase)
        {
            case GamePhase.Start:
                ChangePlayer();
                StartCoroutine(StartPhase());
                break;
            case GamePhase.Queue:
                StartCoroutine(QueuePhase());
                break;
            case GamePhase.Play:
                StartCoroutine(PlayPhase());
                break;
            case GamePhase.Reset:
                StartCoroutine(ResetPhase());
                break;
            case GamePhase.Discover:
                StartCoroutine(DiscoverPhase());
                break;
        }
        UIManager.instance.UpdatePhase();
    }

    void ChangePlayer() {
        if (activePlayer == player)
        {
            activePlayer = opponent;
        }
        else {
            activePlayer = player;
        }
        Log.Write("It is now " + activePlayer._name + "'s turn...");
    }

    IEnumerator StartPhase() {
        currentPhase = GamePhase.Start;
        
        Log.Write("Start Phase...");
        yield return GameManager.instance.medWait;
        if (activePlayer == player)
        {
            UIManager.Popup("Your Turn", Color.black, UIManager.instance.centrePoint, 200f, 0f);

        }
        else {
            UIManager.Popup("Opponent Turn", Color.black, UIManager.instance.centrePoint, 200f, 0f);

        }
        activePlayer.ResetBlock();
        yield return activePlayer.StartCoroutine(activePlayer.PlaySuddens());
        //sudden cards

        Log.Write("End of Start Phase.");
        ChangePhase(GamePhase.Queue);
    }

    IEnumerator QueuePhase() {
        currentPhase = GamePhase.Queue;
        Log.Write("Queue Phase...");

        yield return GameManager.instance.medWait;

        if (activePlayer.queue.cards.Count == 0)
        {
            Log.Write("No queue.");
        }
        else {
            UIManager.Popup("Queue", Color.black, UIManager.instance.centrePoint, 100f, 0f);

            yield return activePlayer.StartCoroutine(activePlayer.StartQueue());
        }
        


        //sudden cards

        Log.Write("End of Queue Phase.");
        ChangePhase(GamePhase.Play);
    }

    IEnumerator PlayPhase()
    {
        currentPhase = GamePhase.Play;
        Log.Write("Play Phase...");
        //UIManager.Popup("Play", Color.black, UIManager.instance.centrePoint, 100f, 0f);

        yield return GameManager.instance.shortWait;

        if (activePlayer == player)
        {
            UIManager.instance.endTurnbutton.interactable = true;
            activePlayer.HideHand(false);
            activePlayer.canPlay = true;

        }
        else {
            //ai goes here?
            //play a random card for now
            yield return StartCoroutine(Ai_Play());
            FinishTurn();
        }
        //player1.ResetBlock();
        //Log.Write("Queue phase does nothing now");
        //yield return new WaitForSeconds(0.5f);


        //sudden cards

        //Log.Write("End of Start Phase.");
    }

    Card FindValidCard(Player player, Card.CardKeyword.Keyword? priority = null)
    {

        int randomOffest = Random.Range(0, player.hand.cards.Count);
        //priority pass
        if (priority != null) {
            for (int i = 0; i < player.hand.cards.Count; ++i)
            {
                Card c;
                if (player.hand.cards.Count == 1)
                {
                    c = player.hand.cards[0];
                }
                else
                {
                    c = player.hand.cards[(i + randomOffest) % (player.hand.cards.Count - 1)];
                }


                //check energy
                if (c.energyCost <= player.energy)
                {
                    //if it's not sudden or unplayable
                    if (c.cardType != Card.CardType.Sudden && c.cardType != Card.CardType.Unplayable)
                    {
                        //if it has our priority
                        Card.CardKeyword priorityKeyword = c.keywords.FirstOrDefault(x => x.keyword == priority);

                        if (priorityKeyword != null)
                        {
                            return c;
                        }
                    }
                }
            }
        }
        


        //play any card pass
        for (int i = 0; i < player.hand.cards.Count; ++i)
        {
            Card c;
            if (player.hand.cards.Count == 1) {
                c = player.hand.cards[0];
            }
            else {
                c = player.hand.cards[(i + randomOffest) % (player.hand.cards.Count - 1)];
            }
             

            //check energy
            if (c.energyCost <= player.energy) {
                //if it's not sudden or unplayable
                if (c.cardType != Card.CardType.Sudden && c.cardType != Card.CardType.Unplayable) {
                    return c;
                }
            }
        }
        return null;

    }
    Card AiDecideCard() {
        if (UIManager.instance.humanAttack <= opponent.block)
        {
            Log.Write("AI PRIORITY IS ATTACK");
            return FindValidCard(opponent, Card.CardKeyword.Keyword.Attack);

        }
        else
        {
            Log.Write("AI PRIORITY IS BLOCK");
            return FindValidCard(opponent, Card.CardKeyword.Keyword.Block);

        }
    }

    IEnumerator Ai_Play() {
        Log.Write("AI PLAY STARTING...");
        Card currentChoice;

        currentChoice = AiDecideCard();

        while (currentChoice != null) {
            yield return currentChoice.StartCoroutine(currentChoice.PlayCoroutine());
            while (!opponent.canPlay) {
                yield return 0;
            }
            yield return GameManager.instance.medWait;
            currentChoice = AiDecideCard();

        }

        Log.Write("AI PLAY FINISHED!");
        //yield return new WaitForSeconds(2.0f);
    }

    IEnumerator ResetPhase()
    {
        currentPhase = GamePhase.Reset;
        if (activePlayer == player) {
            UIManager.instance.endTurnbutton.interactable = false;
            UIManager.Popup("End Turn", Color.black, UIManager.instance.centrePoint, 100f, 0f);

        }

        Log.Write("Reset Phase...");
        yield return GameManager.instance.shortWait;

        //player1.ResetBlock();

        yield return activePlayer.StartCoroutine(activePlayer.ResetCoroutine());


        //sudden cards

        Log.Write("End of Reset Phase.");
        ChangePhase(GamePhase.Discover);
    }

    IEnumerator DiscoverPhase()
    {
        currentPhase = GamePhase.Discover;
        Log.Write("Discover Phase...");
        UIManager.Popup("Discover", Color.black, UIManager.instance.centrePoint, 100f, 0f);

        if (activePlayer == player) {
            activePlayer.HideHand(true);

        }
        yield return GameManager.instance.shortWait;

        
        if (activePlayer == player) {
            UIManager.instance.skipDiscoverButton.gameObject.SetActive(true);
            UIManager.instance.discoveryTitle.SetActive(true);
            activePlayer.Discover();
            yield return GameManager.instance.medWait;
        }
        else{
            if (activePlayer.library.cards.Count > 0) {
                activePlayer.library.cards[0].MoveToPile(activePlayer.discardPile);
            }
            activePlayer.FinishDiscover();
            
        }
        //yield return new WaitForSeconds(0.5f);


        //sudden cards

       // Log.Write("End of Discover Phase.");
        //ChangePhase(GamePhase.Start);
    }

    public void SkipDiscover() {
        player.LoseHealth(2);
        player.FinishDiscover();
    }

    public void PersistentButton() {
        player.SubmitChoice(0);
    }

    public void SetGameSpeed(float speed) {
        UIManager.Popup("Setting GameSpeed to " + speed);
        gameSpeed = speed;

        instance.shortWait = new WaitForSeconds(0.2f * speed);
        instance.medWait = new WaitForSeconds(0.6f * speed);
        instance.longWait = new WaitForSeconds(1.1f * speed);
        instance.fullWait = new WaitForSeconds(2.0f * speed);
    }

    public void EndGame() {
        StopAllCoroutines();
        player.StopAllCoroutines();
        opponent.StopAllCoroutines();
        UIManager.instance.endTurnbutton.interactable = false;
        player.canPlay = false;
        opponent.canPlay = false;
        currentPhase = GamePhase.End;
    }

    public void BackToMenu() {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
