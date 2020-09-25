using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

public class Card : MonoBehaviour
{
    public enum CardType { Fast, Reaction, Sudden, Power, Slow, Unplayable }
    public CardType cardType;
    [ShowIf("cardType", CardType.Slow)]
    public int slowDelay = 0;
    public int energyCost;
    public enum CardCategory {Attack, Defence, Maneuver, Power, Burden }
    public CardCategory category;
    public string cardName = "Card";
    [Multiline]
    public string cardText = "";
    public Sprite art;

    [System.Serializable]
    public class CardKeyword {
        public enum Keyword {Attack, Block, Draw, Strength, Agility, Stamina, Intelligence,
            Heal, LoseHealth, TakeDamage, Stun, StunSelf, Clone, Bounce, Scry, CleanHand,
            Delay, Remember, Discard, GainEnergy, Ignite, IgniteSelf, BouncePower, Give, Concentrate, AddKeyword, AddAttribute,
            OpponentDiscard, OpponentStrength, OpponentAgility, OpponentStamina, OpponentIntelligence, Mill, MillOpponent
        }
        [HorizontalGroup(LabelWidth = 60f)] public Keyword keyword;
        [HorizontalGroup(LabelWidth = 40f)] public int value;
        public string parameters = "";

        public override bool Equals(object obj)
        {
            var keyword = obj as CardKeyword;
            return keyword != null &&
                   this.keyword == keyword.keyword;
        }

        public override int GetHashCode()
        {
            return -1834187100 + keyword.GetHashCode();
        }
    }

    [System.Serializable]
    public class CardAttribute
    {
        public enum Attribute { Fading, Hold, Cycle, Focus, Replenish }
        public Attribute attribute;
        public int value;

        public override bool Equals(object obj)
        {
            var attribute = obj as CardAttribute;
            return attribute != null &&
                   this.attribute == attribute.attribute;
        }

        public override int GetHashCode()
        {
            return -1961618617 + attribute.GetHashCode();
        }
    }

    public enum CardPower {DrawAdditional, Vampire, Inferno, DelaySlows, Napalm, Lesson, Stubborn, MillAttack };
    public List<CardPower> powers;

    public List<CardAttribute> attributes;
    public List<CardKeyword> keywords;

    public enum DiscardType {Discard, Burn, Retain, Persistent }
    public DiscardType discardType = DiscardType.Discard;

    public CardDisplay display;

    public Player owner;
    public Pile currentPile;
    public int currentDelay = 0;

    public bool faceUp = false;

    public bool revealed = false;
    public GameObject revelIcon;

    public void InitFromData(CardData data) {
        this.cardType = data.cardType;
        this.slowDelay = data.slowDelay;
        this.energyCost = data.energyCost;
        this.cardName = data.cardName;

        this.attributes = new List<CardAttribute>();
        foreach (CardAttribute att in data.attributes) {
            this.attributes.Add(new CardAttribute() { attribute = att.attribute, value = att.value });
        }
        this.keywords = new List<CardKeyword>();
        foreach (CardKeyword kw in data.keywords)
        {
            this.keywords.Add(new CardKeyword() { keyword = kw.keyword, value = kw.value, parameters = kw.parameters });
        }
        this.discardType = data.discardType;
        this.powers = new List<CardPower>(data.powers);

        
        this.cardText = data.cardText;

        this.category = data.category;

        this.art = data.art;
    }

    public CardData GetData() {
        CardData data = new CardData();

        data.cardType = this.cardType;
        data.slowDelay = this.slowDelay;
        data.energyCost = this.energyCost;
        data.cardName = this.cardName;

        data.attributes = new List<CardAttribute>(this.attributes);
        data.keywords = new List<CardKeyword>(this.keywords);
        data.discardType = this.discardType;
        data.powers = new List<CardPower>(this.powers);

        data.cardText = this.cardText;


        data.art = this.art;

        data.category = this.category;

        return data;
    }

    void Start() {
        UpdateDisplay();

        //if we're sudden, our energy cost must be 0
        if (cardType == CardType.Sudden) energyCost = 0;

        //get pile if we can't find it
        //if (currentPile == null) {
        //    if (transform.parent.GetComponent<Pile>() != null) {
        //        currentPile = transform.parent.GetComponent<Pile>();
        //    }
        //}
    }

    public void OnDraw(bool allowSudden = true) {
        //i've been drawn
        Log.Write("Drew card: " + cardName);
        if(owner == GameManager.instance.player)SetFacing(true);
        CardAttribute replenishAttribute = attributes.FirstOrDefault(x => x.attribute == CardAttribute.Attribute.Replenish);
        if (replenishAttribute != null) {
            Log.Write("Replenish");
            //UIManager.Popup("Replenish");
            owner.StartCoroutine(owner.StartDrawCoroutine(1));
            SetReveal(true);
        }

        if (allowSudden && cardType == CardType.Sudden) {
            Play();
        }
    }

    public Vector3 desiredPosition;
    public Space desiredPosSpace = Space.Self;

    public void Update() {
        UpdatePosition();

    }

    public bool GetIsPlayable() {
        if (owner != GameManager.instance.player) return false;
        if (owner.energy < energyCost) return false;
        if (cardType == CardType.Sudden || cardType == CardType.Unplayable) return false;
        return true;
    }

    string altText;
    public string GetAltText() {
        return altText;
    }

    public void UpdateDisplay() {
        display.gameObject.SetActive(faceUp);
        display.UpdateDisplay();
    }

    public void SetFacing(bool up) {
        if (!revealed)
        {
            faceUp = up;
        }
        else {
            faceUp = true;
        }
        
        UpdateDisplay();
    }

    public void SetReveal(bool _revealed) {
        revealed = _revealed;
        if (revealed) {
            SetFacing(true);
        }
        if(!owner.human) revelIcon.SetActive(_revealed);
    }

    void UpdatePosition() {
        switch (desiredPosSpace)
        {
            case Space.World:
                if (Vector3.Distance(transform.position, desiredPosition) < 0.01f)
                {
                    transform.position = desiredPosition;
                }
                else {
                    transform.position = Vector3.Lerp(transform.position, desiredPosition, 10f * Time.deltaTime);
                }

                break;
            case Space.Self:

                if (Vector3.Distance(transform.localPosition, desiredPosition) < 0.01f)
                {
                    transform.localPosition = desiredPosition;
                }
                else
                {
                    transform.localPosition = Vector3.Lerp(transform.localPosition, desiredPosition, 10f * Time.deltaTime);
                }

                break;
        }
    }

    public void Play(bool alternate = false) {
        StartCoroutine(PlayCoroutine(alternate));
    }

    public IEnumerator PlayCoroutine(bool alternate = false) {
        if (GameManager.instance.currentPhase != GameManager.GamePhase.Play)
        {
            Log.Write("Can't play this now");
            yield break;
        }


        if (alternate)
        {
            //check for attributes
            #region focusChoice
            CardAttribute focusAttribute = attributes.FirstOrDefault(x => x.attribute == CardAttribute.Attribute.Focus);
            if (focusAttribute != null)
            {
                if (owner.human)
                {
                    //card has focus, start a choice
                    MoveToPile(GameManager.instance.playPile);

                    yield return owner.StartCoroutine(owner.StartChoice(
                        "Play",
                        "Gain " + focusAttribute.value.ToString() + " energy",
                        "Focus " + focusAttribute.value.ToString() + " : Discard this card to gain x energy?",
                        owner.energy >= energyCost
                        ));

                    int result = owner.GetChoiceResult();
                    if (result == 0)
                    {
                        //cancelled
                        Log.Write("Cancel.");
                        MoveToPile(owner.hand); //do this for now, may need a better way to cancel
                        owner.canPlay = true;
                        yield break;
                    }
                    else if (result == 1)
                    {
                        Log.Write("Play chosen.");
                        //Nothing to do?
                    }
                    else if (result == 2)
                    {
                        Log.Write("Focus Chosen.");
                        owner.GainEnergy(focusAttribute.value);
                        UIManager.Popup("Focus" + focusAttribute.value);
                        MoveToPile(owner.discardPile);
                        owner.canPlay = true;

                        yield break;
                    }
                }
                else
                {
                    Log.Write("Ai doesn't use focus.");

                }
            }
            #endregion

            #region cycleChoice
            CardAttribute cycleAttribute = attributes.FirstOrDefault(x => x.attribute == CardAttribute.Attribute.Cycle);
            if (cycleAttribute != null)
            {
                if (owner.human)
                {
                    //card has focus, start a choice
                    MoveToPile(GameManager.instance.playPile);

                    yield return owner.StartCoroutine(owner.StartChoice(
                        "Play",
                        "Pay " + cycleAttribute.value.ToString() + " energy",
                        "Cycle " + cycleAttribute.value.ToString() + " : Discard this card and pay " + cycleAttribute.value.ToString() + " energy to draw a card?",
                        owner.energy >= energyCost,
                        owner.energy >= cycleAttribute.value
                        ));

                    int result = owner.GetChoiceResult();
                    if (result == 0)
                    {
                        //cancelled
                        Log.Write("Cancel.");
                        MoveToPile(owner.hand); //do this for now, may need a better way to cancel
                        owner.canPlay = true;
                        yield break;
                    }
                    else if (result == 1)
                    {
                        Log.Write("Play chosen.");
                        //Nothing to do?
                    }
                    else if (result == 2)
                    {
                        Log.Write("Cycle Chosen.");
                        owner.GainEnergy(-cycleAttribute.value);
                        UIManager.Popup("Cycle" + cycleAttribute.value);
                        MoveToPile(owner.discardPile);
                        yield return owner.StartCoroutine(owner.StartDrawCoroutine(1));
                        owner.canPlay = true;

                        yield break;
                    }
                }
                else
                {
                    Log.Write("Ai doesn't use cycle.");

                }
            }
            #endregion 
        }

        if (owner.energy < energyCost) {
            Log.Write("Not enough energy!");
            if (owner == GameManager.instance.player) UIManager.Popup("Not enough energy!");
            yield break;
        }

        SetFacing(true);
        

        Log.Write(cardName + "(" + cardType.ToString() + ") played");



        switch (cardType)
        {
            case CardType.Fast:
                owner.canPlay = false;
                owner.energy -= energyCost;
                if (owner == GameManager.instance.player)
                {
                    UIManager.Popup("-" + energyCost, UIManager.instance.energyText.transform.position, Color.cyan);
                }
                else
                {
                    UIManager.Popup("-" + energyCost, UIManager.instance.opponentEnergy.transform.position, Color.cyan);
                }
                Resolve();
                break;
            case CardType.Reaction:
                break;
            case CardType.Sudden:
                owner.canPlay = false;
                Resolve();
                break;
            case CardType.Power:
                owner.energy -= energyCost;
                if (owner == GameManager.instance.player)
                {
                    UIManager.Popup("-" + energyCost, UIManager.instance.energyText.transform.position, Color.cyan);
                }
                else
                {
                    UIManager.Popup("-" + energyCost, UIManager.instance.opponentEnergy.transform.position, Color.cyan);
                }

                MoveToPile(owner.powers);
                foreach (CardPower power in powers) {
                    RegisterPower(power, true);
                }

                break;
            case CardType.Slow:
                owner.energy -= energyCost;
                if (owner == GameManager.instance.player)
                {
                    UIManager.Popup("-" + energyCost, UIManager.instance.energyText.transform.position, Color.cyan);
                }
                else
                {
                    UIManager.Popup("-" + energyCost, UIManager.instance.opponentEnergy.transform.position, Color.cyan);
                }
                currentDelay = slowDelay;
                UpdateDisplay();
                if (owner.enemy.interrupts.delaySlowsInterrupt != null) {
                    MoveToPile(GameManager.instance.playPile);
                    owner.enemy.interrupts.delaySlowsInterrupt.MoveToPile(GameManager.instance.playPile);
                    yield return owner.enemy.StartCoroutine(owner.enemy.StartChoice("Decline", "Pay 1", "Pay 1 energy to delay this card 1", true, owner.enemy.energy >= 1));
                    int result = owner.enemy.GetChoiceResult();
                    if (result == 0)
                    {
                        //cancelled
                    }
                    else if (result == 1)
                    {
                        //declined
                    }
                    else {
                        //accepted;
                        owner.enemy.energy -= 1;
                        currentDelay += 1;
                        UpdateDisplay();
                    }
                    owner.enemy.interrupts.delaySlowsInterrupt.MoveToPile(owner.enemy.powers);
                }

                MoveToPile(owner.queue);
                owner.canPlay = true;
                break;
            case CardType.Unplayable:
                //unplayable
                break;
        }
        UIManager.UpdateUI();
    }

    public void RegisterPower(CardPower power, bool register = true) {
        Card reg = register ? this : null;

        switch (power)
        {
            case CardPower.DrawAdditional:
                if (owner.interrupts.drawAdditionalInterrupt != null)
                {
                    owner.interrupts.drawAdditionalInterrupt.MoveToPile(owner.burnPile);
                    UIManager.Popup("You already have this power in play!");
                }

                owner.interrupts.drawAdditionalInterrupt = reg;
                break;
            case CardPower.Vampire:
                if (owner.enemy.interrupts.vampireInterrupt != null)
                {
                    owner.enemy.interrupts.vampireInterrupt.MoveToPile(owner.enemy.burnPile);
                    UIManager.Popup("You already have this power in play!");
                }

                owner.enemy.interrupts.vampireInterrupt = reg;
                break;
            case CardPower.Inferno:
                if (owner.interrupts.infernoInterrupt != null)
                {
                    owner.interrupts.infernoInterrupt.MoveToPile(owner.burnPile);
                    UIManager.Popup("You already have this power in play!");
                }

                owner.interrupts.infernoInterrupt = reg;
                break;
            case CardPower.DelaySlows:
                if (owner.interrupts.delaySlowsInterrupt != null)
                {
                    owner.interrupts.delaySlowsInterrupt.MoveToPile(owner.burnPile);
                    UIManager.Popup("You already have this power in play!");
                }

                owner.interrupts.delaySlowsInterrupt = reg;
                break;
            case CardPower.Napalm:
                if (owner.interrupts.napalmInterrupt != null)
                {
                    owner.interrupts.napalmInterrupt.MoveToPile(owner.burnPile);
                    UIManager.Popup("You already have this power in play!");
                }

                owner.interrupts.napalmInterrupt = reg;
                break;
            case CardPower.Lesson:
                if (owner.interrupts.lessonInterrupt != null)
                {
                    owner.interrupts.lessonInterrupt.MoveToPile(owner.burnPile);
                    UIManager.Popup("You already have this power in play!");
                }

                owner.interrupts.lessonInterrupt = reg;
                break;
            case CardPower.Stubborn:
                if (owner.interrupts.stubbornInterrupt != null)
                {
                    owner.interrupts.stubbornInterrupt.MoveToPile(owner.burnPile);
                    UIManager.Popup("You already have this power in play!");
                }
                if (register)
                {
                    owner.GainIntelligence(-4);
                }
                else
                {
                    owner.GainIntelligence(4);
                }
                owner.interrupts.stubbornInterrupt = reg;
                break;
            case CardPower.MillAttack:
                if (owner.interrupts.millAttackInterrupt != null)
                {
                    owner.interrupts.millAttackInterrupt.MoveToPile(owner.burnPile);
                    UIManager.Popup("You already have this power in play!");
                }
                
                owner.interrupts.millAttackInterrupt = reg;
                break;
        }
    }

    public void Resolve() {


        //resolve the card here
        //remove the card from it's pile
        currentPile.cards.Remove(this);
        currentPile.UpdateCards();
        currentPile = null;
        StartCoroutine(ResolveCoroutine());
    }

    public IEnumerator ResolveCoroutine() {
        Log.Write(cardName + " resolving");
        MoveToPile(GameManager.instance.playPile);
        yield return GameManager.instance.medWait;

        foreach (CardKeyword kw in keywords) {
            yield return StartCoroutine(ResolveKeyword(kw));
        }

        //finished resolving
        if(cardType != CardType.Slow) owner.canPlay = true;
        switch (discardType)
        {
            case DiscardType.Discard:
                Discard();
                break;
            case DiscardType.Burn:
                yield return StartCoroutine(Burn());
                break;
            case DiscardType.Retain:
                Retain();
                break;
            case DiscardType.Persistent:
                Persist();
                break;
        }
    }

    public void Discard() {
        Log.Write(cardName + " discarding");
        MoveToPile(owner.discardPile);

    }

    public IEnumerator Burn() {
        Log.Write(cardName + " burning");
        if (owner == GameManager.instance.player) UIManager.Popup("Burn");
        display.burnEffect.Stop();
        display.burnEffect.Play();
        yield return GameManager.instance.shortWait;


        MoveToPile(owner.burnPile);
        
    }

    public void Retain() {
        Log.Write(cardName + " retaining");
        if (owner == GameManager.instance.player) UIManager.Popup("Retain");

        MoveToPile(owner.hand);
    }

    public void Persist()
    {
        Log.Write(cardName + " persisting");
        if (owner == GameManager.instance.player) UIManager.Popup("Persist");

        MoveToPile(owner.queue);
    }

    public void MoveToPile(Pile target, bool top = true) {
        //Log.Write("Moving from " + currentPile + " to " + target);

        Pile oldpile = currentPile;

        if (top)
        {
            target.cards.Add(this);
        }
        else {
            target.cards.Insert(0, this);
        }

        if (currentPile != null) {
            currentPile.cards.Remove(this);


            currentPile.UpdateCards();
        }
        
        target.UpdateCards();

        //manage queueattacktext

        if (target == owner.queue || oldpile == owner.queue) {
            UIManager.instance.UpdateQueueAttackText();
        }

        if (cardType == CardType.Power && oldpile == owner.powers && currentPile != GameManager.instance.playPile) {
            foreach (CardPower power in powers) {
                RegisterPower(power, false);
            }
        }

    }

    public string GenerateCardText() {
        if (cardText != "") return cardText;


        string result = "";

        foreach (CardAttribute att in attributes)
        {
            result += "[" + att.attribute.ToString();
            if (att.value != 0) {
                result += " " + att.value.ToString();
            }
            result += "]\n";
        }

        //alt text
        CardAttribute focusAttribute = attributes.FirstOrDefault(x => x.attribute == CardAttribute.Attribute.Focus);
        CardAttribute cycleAttribute = attributes.FirstOrDefault(x => x.attribute == CardAttribute.Attribute.Cycle);
        if (focusAttribute != null) altText = "Focus " + focusAttribute.value.ToString();
        if (cycleAttribute != null) altText = "Cycle " + cycleAttribute.value.ToString();


        foreach (CardKeyword kw in keywords) {
            string kwText = GetKeywordCardText(kw);
            if (kwText == "")
            {
                result += kw.keyword.ToString() + " " + kw.value.ToString();
            }
            else {
                result += kwText;
            }
            result += "\n";
        }

        result += "\n<align=\"center\">";

        switch (discardType)
        {
            case DiscardType.Discard:
                break;
            case DiscardType.Burn:
                result += GetSprite("burn") + " Burn";
                break;
            case DiscardType.Retain:
                result += "Retain";
                break;
            case DiscardType.Persistent:
                result += "Persistent";

                break;
        }

        return result;
    }

    const string positiveColor = "<#005500>";
    const string negativeColor = "<#550000>";
    const string endColor = "</color>";

    public Card(CardType cardType, int slowDelay, int energyCost, string cardName, string cardText, Sprite art, List<CardPower> powers, List<CardAttribute> attributes, List<CardKeyword> keywords, DiscardType discardType, CardDisplay display, Player owner, Pile currentPile, int currentDelay, bool faceUp, bool revealed, GameObject revelIcon, Vector3 desiredPosition, Space desiredPosSpace, string altText, CardCategory category)
    {
        this.cardType = cardType;
        this.slowDelay = slowDelay;
        this.energyCost = energyCost;
        this.cardName = cardName;
        this.cardText = cardText;
        this.art = art;
        this.powers = powers;
        this.attributes = new List<CardAttribute>();
        foreach (CardAttribute att in attributes)
        {
            this.attributes.Add(new CardAttribute() { attribute = att.attribute, value = att.value });
        }
        this.keywords = new List<CardKeyword>();
        foreach (CardKeyword kw in keywords)
        {
            this.keywords.Add(new CardKeyword() { keyword = kw.keyword, value = kw.value, parameters = kw.parameters });
        }
        this.discardType = discardType;
        this.display = display;
        this.owner = owner;
        this.currentPile = currentPile;
        this.currentDelay = currentDelay;
        this.faceUp = faceUp;
        this.revealed = revealed;
        this.revelIcon = revelIcon;
        this.desiredPosition = desiredPosition;
        this.desiredPosSpace = desiredPosSpace;
        this.altText = altText;
        this.category = category;
    }

    public Card(CardData data)
    {
        this.cardType = data.cardType;
        this.slowDelay = data.slowDelay;
        this.energyCost = data.energyCost;
        this.cardName = data.cardName;

        this.attributes = new List<CardAttribute>();
        foreach (CardAttribute att in data.attributes)
        {
            this.attributes.Add(new CardAttribute() { attribute = att.attribute, value = att.value });
        }
        this.keywords = new List<CardKeyword>();
        foreach (CardKeyword kw in data.keywords)
        {
            this.keywords.Add(new CardKeyword() { keyword = kw.keyword, value = kw.value, parameters = kw.parameters });
        }
        this.discardType = data.discardType;
        this.powers = new List<CardPower>(data.powers);


        this.cardText = data.cardText;
        this.category = data.category;

        this.art = data.art;
    }

    public string GetKeywordCardText(CardKeyword kw) {
        //if we're not in the game
        if (owner == null) owner = new Player();

        switch (kw.keyword)
        {
            case CardKeyword.Keyword.Attack:
                if (owner.strength > 0)
                {
                    return GetSprite("attack") + "Attack " + positiveColor + (kw.value + owner.strength) + "<size=0.08>(+" + owner.strength.ToString() + ")</size></color>";
                }
                else if (owner.strength < 0)
                {
                    return GetSprite("attack") + "Attack " + positiveColor + (kw.value + owner.strength) + "<size=0.08>(" + owner.strength.ToString() + ")</size></color>";
                }
                else
                {
                    return GetSprite("attack") + "Attack " + kw.value;
                }

            case CardKeyword.Keyword.Block:
                if (owner.agility > 0)
                {
                    return GetSprite("block") + "Block " + positiveColor + (kw.value + owner.agility) + "<size=0.08>(+" + owner.agility.ToString() + ")</size></color>";

                }
                else if (owner.agility < 0)
                {
                    return GetSprite("block") + "Block " + negativeColor + (kw.value + owner.agility) + "<size=0.08>(" + owner.agility.ToString() + ")</size></color>";
                }
                else
                {
                    return GetSprite("block") + "Block " + kw.value;
                }
            case CardKeyword.Keyword.Draw:
                return "";
            case CardKeyword.Keyword.Strength:
                if (kw.value >= 0)
                {
                    return positiveColor + "Gain " + kw.value + " " + GetSprite("strength") + "Strength</color>";
                }
                else
                {
                    return negativeColor + "Lose " + Mathf.Abs(kw.value) + " " + GetSprite("strength") + "Strength</color>";
                }
            case CardKeyword.Keyword.Agility:
                if (kw.value >= 0)
                {
                    return positiveColor + "Gain " + kw.value + " " + GetSprite("agility") + "Agility</color>";
                }
                else
                {
                    return negativeColor + "Lose " + Mathf.Abs(kw.value) + " " + GetSprite("agility") + "Agility</color>";
                }
            case CardKeyword.Keyword.Stamina:
                if (kw.value >= 0)
                {
                    return positiveColor + "Gain " + kw.value + " " + GetSprite("stamina") + "Stamina</color>";
                }
                else
                {
                    return negativeColor + "Lose " + Mathf.Abs(kw.value) + " " + GetSprite("stamina") + "Stamina</color>";
                }
            case CardKeyword.Keyword.Intelligence:
                if (kw.value >= 0)
                {
                    return positiveColor + "Gain " + kw.value + " " + GetSprite("intelligence") + "Intelligence</color>";
                }
                else
                {
                    return negativeColor + "Lose " + Mathf.Abs(kw.value) + " " + GetSprite("intelligence") + "Intelligence</color>";
                }
            case CardKeyword.Keyword.Heal:
                return positiveColor + GetSprite("heal") + "Heal " + kw.value + endColor;
            case CardKeyword.Keyword.LoseHealth:
                return negativeColor + "Lose " + kw.value + " Health" + endColor;
            case CardKeyword.Keyword.TakeDamage:
                return negativeColor + "Take " + kw.value + " Damage" + endColor;
            case CardKeyword.Keyword.Stun:
                return "";
            case CardKeyword.Keyword.StunSelf:
                return negativeColor + "Stun yourself " + kw.value + endColor;
            case CardKeyword.Keyword.Clone:
                return "";
            case CardKeyword.Keyword.Bounce:
                return "Return up to" + kw.value + " card" + (kw.value == 1 ? "" : "s") + " from your opponent's queue to their hand.";
            case CardKeyword.Keyword.Scry:
                return "";
            case CardKeyword.Keyword.CleanHand:
                return "Burn " + kw.value + " card" + (kw.value == 1 ? "" : "s") + " from your hand";
            case CardKeyword.Keyword.Delay:
                return "";
            case CardKeyword.Keyword.Remember:
                return "Return " + kw.value + " cards from your discard pile to your hand.";
            case CardKeyword.Keyword.Discard:
                return negativeColor + "Discard " + kw.value + endColor;
            case CardKeyword.Keyword.GainEnergy:
                return positiveColor + "Gain " + kw.value + " Energy" + endColor;
            case CardKeyword.Keyword.Ignite:
                return "";
            case CardKeyword.Keyword.BouncePower:
                return "Return " + kw.value + " power cards in play to their owners hand.";
            case CardKeyword.Keyword.Give:
                return "Give " + kw.value + " cards from your hand to your opponent.";
            case CardKeyword.Keyword.Concentrate:
                return "Burn " + kw.value + " cards from your hand, gain energy equal to it's cost.";
            case CardKeyword.Keyword.IgniteSelf:
                return "Ignite yourself " + kw.value;
            case CardKeyword.Keyword.AddKeyword:
                return "Give " + kw.value + " cards in your hand " + kw.parameters;
            case CardKeyword.Keyword.AddAttribute:
                return "Give " + kw.value + " cards in your hand " + kw.parameters;
            case CardKeyword.Keyword.OpponentDiscard:
                return "Opponent Discard " + kw.value;
            case CardKeyword.Keyword.OpponentStrength:
                return "Opponent Strength " + kw.value;
            case CardKeyword.Keyword.OpponentAgility:
                return "Opponent Agility " + kw.value;
            case CardKeyword.Keyword.OpponentStamina:
                return "Opponent Stamina " + kw.value;
            case CardKeyword.Keyword.OpponentIntelligence:
                return "Opponent Intelligence " + kw.value;
            case CardKeyword.Keyword.Mill:
                return negativeColor + "Mill " + kw.value + endColor;
            case CardKeyword.Keyword.MillOpponent:
                return "Opponent Mill " + kw.value;
            default:
                Debug.LogWarning("Keyword " + kw.keyword + " " + kw.value + " has no card text defined");
                return "";
        }
    }

    string GetSprite(string sprite) {
        return "<sprite name=\"" + sprite + "\" tint=1>";
    }

    public string GetKeywordDescription(CardKeyword kw) {
        switch (kw.keyword)
        {
            case CardKeyword.Keyword.Attack:
                return "Attack x: Deal (x + strength) damage.";
            case CardKeyword.Keyword.Block:
                return "Block x: Gain (x + agility) block.";
            case CardKeyword.Keyword.Draw:
                return "Draw x: Draw x cards.";
            case CardKeyword.Keyword.Strength:
                return "Strength increases attack damage.";
            case CardKeyword.Keyword.Agility:
                return "Agility increases block amount.";
            case CardKeyword.Keyword.Stamina:
                return "At the end of your turn, set your energy to your stamina level.";
            case CardKeyword.Keyword.Intelligence:
                return "At the end of your turn, after discarding your hand, draw cards equal to your intelligence.";
            case CardKeyword.Keyword.Heal:
                return "Heal x: Gain x health.";
            case CardKeyword.Keyword.LoseHealth:
                return "Lose x health.";
            case CardKeyword.Keyword.TakeDamage:
                return "Take x damage. Reduced by block.";
            case CardKeyword.Keyword.Stun:
                return "Stun x: Add x 'Stunned' cards to your opponent's discard pile.";
            case CardKeyword.Keyword.StunSelf:
                return "Stun self x: Add x 'Stunned' cards to your discard pile.";
            case CardKeyword.Keyword.Clone:
                return "Clone x: Add x copies of this card to your discard pile";
            case CardKeyword.Keyword.Bounce:
                return "Bounce x: Put up to x cards from your opponents queue back into their hand";
            case CardKeyword.Keyword.Scry:
                return "Scry x: Look at the top x cards of your draw pile, discard any of them.";
            case CardKeyword.Keyword.CleanHand:
                break;
            case CardKeyword.Keyword.Delay:
                return "Delay x: Delay a card in your opponent's queue by x";
            case CardKeyword.Keyword.Remember:
                return "Remember x: Return up to x cards from your discard pile to your hand.";
            case CardKeyword.Keyword.Discard:
                break;
            case CardKeyword.Keyword.GainEnergy:
                break;
            case CardKeyword.Keyword.Ignite:
                return "Ignite x: Add x 'Fire' cards to your opponent's draw pile";
            case CardKeyword.Keyword.IgniteSelf:
                return "Ignite x: Add x 'Fire' cards to your draw pile";
            case CardKeyword.Keyword.BouncePower:
                break;
            case CardKeyword.Keyword.Give:
                break;
            case CardKeyword.Keyword.Concentrate:
                break;
            case CardKeyword.Keyword.AddKeyword:
                break;
            case CardKeyword.Keyword.AddAttribute:
                break;
            case CardKeyword.Keyword.OpponentDiscard:
                break;
            case CardKeyword.Keyword.OpponentStrength:
                break;
            case CardKeyword.Keyword.OpponentAgility:
                break;
            case CardKeyword.Keyword.OpponentStamina:
                break;
            case CardKeyword.Keyword.OpponentIntelligence:
                break;
            case CardKeyword.Keyword.Mill:
            case CardKeyword.Keyword.MillOpponent:
                return "Mill x: Draw and burn x cards.";
            default:
                return "";
        }
        return "";
    }

    public string GetAttributeDescription(CardAttribute att) {
        switch (att.attribute)
        {
            case CardAttribute.Attribute.Fading:
                return "At the end of your turn, if this card is still in your hand, it burns.";
            case CardAttribute.Attribute.Hold:
                return "At the end of your turn, do not discard this card";
            case CardAttribute.Attribute.Cycle:
                return "Cycle x: You may discard this card and pay x energy to draw a card.";
            case CardAttribute.Attribute.Focus:
                return "Focus x: You may discard this card to gain x energy";
            case CardAttribute.Attribute.Replenish:
                return "When you draw this card, draw another card.";
            default:
                return "This attribute has no description";
        }
    }

    public IEnumerator ResolveKeyword(CardKeyword kw) {
        Log.Write("Resolving keyword: " + kw.keyword.ToString() + " " + kw.value.ToString());

        switch (kw.keyword)
        {
            case CardKeyword.Keyword.Attack:
                owner.StartCoroutine(owner.Attack(kw.value));
                break;
            case CardKeyword.Keyword.Block:
                owner.Block(kw.value);
                break;
            case CardKeyword.Keyword.Draw:

                yield return owner.StartCoroutine(owner.StartDrawCoroutine(kw.value));
                break;
            case CardKeyword.Keyword.Strength:
                owner.GainStrength(kw.value);
                break;
            case CardKeyword.Keyword.Agility:
                owner.GainAgility(kw.value);
                break;
            case CardKeyword.Keyword.Stamina:
                owner.GainStamina(kw.value);
                break;
            case CardKeyword.Keyword.Intelligence:
                owner.GainIntelligence(kw.value);
                break;
            case CardKeyword.Keyword.Heal:
                owner.Heal(kw.value);
                break;
            case CardKeyword.Keyword.LoseHealth:
                owner.LoseHealth(kw.value);
                break;
            case CardKeyword.Keyword.TakeDamage:
                owner.TakeDamage(kw.value);
                break;
            case CardKeyword.Keyword.Stun:
                yield return owner.StartCoroutine(owner.StunCoroutine(kw.value, owner.enemy));
                break;
            case CardKeyword.Keyword.StunSelf:
                yield return owner.StartCoroutine(owner.StunCoroutine(kw.value, owner));
                break;
            case CardKeyword.Keyword.Clone:
                for (int i = 0; i < kw.value; ++i)
                {
                    yield return owner.StartCoroutine(owner.SpawnCard(this.GetData(), owner, owner.discardPile));

                }
                break;
            case CardKeyword.Keyword.Bounce:
                for (int i = 0; i < kw.value; ++i)
                {

                    GameManager.instance.playPile.Hide(true);
                    yield return owner.StartCoroutine(owner.StartCardSelect("Bounce " + (kw.value - i) + " cards from your opponent's queue", owner.enemy.queue, "Done"));
                    if (owner.selectedCard != null)
                    {
                        Log.Write("Bounce card selected");
                        owner.selectedCard.MoveToPile(owner.selectedCard.owner.hand);
                        owner.selectedCard = null;
                    }

                    GameManager.instance.playPile.Hide(false);



                }
                owner.cardSelectingPile = new List<Pile>();
                break;
            case CardKeyword.Keyword.Scry:
                //hide the play card
                if (owner.drawPile.cards.Count == 0)
                {
                    UIManager.Popup("No cards in draw pile!");
                    break;
                }

                GameManager.instance.playPile.Hide(true);
                yield return owner.StartCoroutine(owner.StartScry(kw.value));
                GameManager.instance.playPile.Hide(false);
                owner.cardSelectingPile = new List<Pile>();


                break;
            case CardKeyword.Keyword.CleanHand:
                for (int i = 0; i < kw.value; ++i)
                {

                    GameManager.instance.playPile.Hide(true);
                    yield return owner.StartCoroutine(owner.StartCardSelect("Burn " + (kw.value - i) + " cards from your hand", owner.hand));
                    if (owner.selectedCard != null)
                    {
                        Log.Write("Burn card selected");
                        owner.selectedCard.MoveToPile(owner.burnPile);
                        owner.selectedCard = null;
                    }

                    GameManager.instance.playPile.Hide(false);



                }
                owner.cardSelectingPile = new List<Pile>();
                break;
            case CardKeyword.Keyword.Delay:
                GameManager.instance.playPile.Hide(true);
                yield return owner.StartCoroutine(owner.StartCardSelect("Add " + kw.value + " dealy to a card in your opponent's queue", owner.enemy.queue, "Skip"));
                if (owner.selectedCard != null)
                {
                    Log.Write("DelayCardSelected");
                    owner.selectedCard.currentDelay += kw.value;
                    owner.selectedCard.UpdateDisplay();
                    owner.selectedCard = null;
                }
                owner.cardSelectingPile = new List<Pile>();
                GameManager.instance.playPile.Hide(false);

                break;
            case CardKeyword.Keyword.Remember:
                for (int i = 0; i < kw.value; ++i)
                {

                    GameManager.instance.playPile.Hide(true);
                    owner.discardPile.Transfer(owner.viewer);
                    yield return owner.StartCoroutine(owner.StartCardSelect("Put " + (kw.value - i) + " cards from your discard into your hand", owner.viewer, "Done"));
                    if (owner.selectedCard != null)
                    {
                        Log.Write("remember card selected");
                        owner.selectedCard.MoveToPile(owner.hand);
                        owner.selectedCard = null;
                    }
                    owner.cardSelectingPile = new List<Pile>();
                    owner.viewer.Transfer(owner.discardPile);
                    GameManager.instance.playPile.Hide(false);



                }

                break;
            case CardKeyword.Keyword.Discard:
                for (int i = 0; i < kw.value; ++i)
                {

                    GameManager.instance.playPile.Hide(true);
                    yield return owner.StartCoroutine(owner.StartCardSelect("Discard " + (kw.value - i) + " cards", owner.hand));
                    if (owner.selectedCard != null)
                    {
                        Log.Write("discard card selected");
                        owner.selectedCard.MoveToPile(owner.discardPile);
                        owner.selectedCard = null;
                    }
                    owner.cardSelectingPile = new List<Pile>();
                    GameManager.instance.playPile.Hide(false);



                }

                break;
            case CardKeyword.Keyword.GainEnergy:
                owner.GainEnergy(kw.value);

                break;
            case CardKeyword.Keyword.Ignite:
                for (int i = 0; i < kw.value; ++i)
                {
                    yield return owner.StartCoroutine(owner.SpawnCard(GameManager.instance.fireCard, owner.enemy, owner.enemy.drawPile));

                }
                break;
            case CardKeyword.Keyword.IgniteSelf:
                for (int i = 0; i < kw.value; ++i)
                {
                    yield return owner.StartCoroutine(owner.SpawnCard(GameManager.instance.fireCard, owner, owner.drawPile));

                }
                break;
            case CardKeyword.Keyword.BouncePower:
                for (int i = 0; i < kw.value; ++i)
                {

                    GameManager.instance.playPile.Hide(true);
                    yield return owner.StartCoroutine(owner.StartCardSelect("Bounce " + (kw.value - i) + " power cards from play", new List<Pile>() { owner.enemy.powers, owner.powers }, "Done"));
                    if (owner.selectedCard != null)
                    {
                        Log.Write("Bounce card selected");
                        owner.selectedCard.MoveToPile(owner.selectedCard.owner.hand);
                        owner.selectedCard.SetReveal(true);
                        owner.selectedCard = null;
                    }

                    GameManager.instance.playPile.Hide(false);



                }
                owner.cardSelectingPile = new List<Pile>();
                break;
            case CardKeyword.Keyword.Give:
                for (int i = 0; i < kw.value; ++i)
                {

                    GameManager.instance.playPile.Hide(true);
                    yield return owner.StartCoroutine(owner.StartCardSelect("Give " + (kw.value - i) + " cards from your hand to your opponent", owner.hand));
                    if (owner.selectedCard != null)
                    {
                        Log.Write("give card selected");
                        owner.selectedCard.owner = owner.enemy;
                        owner.selectedCard.SetReveal(true);
                        owner.selectedCard.MoveToPile(owner.enemy.hand);
                        owner.selectedCard = null;
                    }

                    GameManager.instance.playPile.Hide(false);



                }
                owner.cardSelectingPile = new List<Pile>();
                break;
            case CardKeyword.Keyword.Concentrate:
                for (int i = 0; i < kw.value; ++i)
                {

                    GameManager.instance.playPile.Hide(true);
                    yield return owner.StartCoroutine(owner.StartCardSelect("Burn " + (kw.value - i) + " cards from your hand, gain energy equal to their cost.", owner.hand));
                    if (owner.selectedCard != null)
                    {
                        Log.Write("concentrate card selected");
                        owner.GainEnergy(owner.selectedCard.energyCost);
                        owner.selectedCard.MoveToPile(owner.burnPile);
                        owner.selectedCard = null;
                    }

                    GameManager.instance.playPile.Hide(false);



                }
                owner.cardSelectingPile = new List<Pile>();
                break;
            case CardKeyword.Keyword.AddKeyword:
                //parse parameters
                string[] split = kw.parameters.Split(' ');
                string keyword = split[0];
                string value = split[1];

                CardKeyword.Keyword parseKw;
                int parseValue;
                if (System.Enum.TryParse(keyword, out parseKw))
                {
                    if (int.TryParse(value, out parseValue))
                    {
                        //success
                    }
                    else
                    {
                        Debug.LogError("Parse Failed!");
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError("Parse Failed!");
                    yield break;
                }

                CardKeyword parsed = new CardKeyword() { keyword = parseKw, value = parseValue };

                if (kw.value == 0)
                {
                    //0 is all
                    foreach (Card c in owner.hand.cards)
                    {
                        c.keywords.Add(parsed);
                    }
                }
                else
                {
                    for (int i = 0; i < kw.value; ++i)
                    {

                        GameManager.instance.playPile.Hide(true);
                        yield return owner.StartCoroutine(owner.StartCardSelect("Choose " + (kw.value - i) + " cards from your hand, give them " + parsed.keyword.ToString() + " " + parsed.value.ToString(), owner.hand, "Done"));
                        if (owner.selectedCard != null)
                        {
                            Log.Write("addkeyword card selected");
                            //owner.GainEnergy(owner.selectedCard.energyCost);
                            owner.selectedCard.keywords.Add(parsed);
                            owner.selectedCard.UpdateDisplay();
                            owner.selectedCard = null;
                        }

                        GameManager.instance.playPile.Hide(false);



                    }
                    owner.cardSelectingPile = new List<Pile>();
                }



                break;
            case CardKeyword.Keyword.AddAttribute:
                //parse parameters
                string[] a_split = kw.parameters.Split(' ');
                string a_att = a_split[0];
                string a_value = a_split[1];

                CardAttribute.Attribute parseAtt;
                int a_parseValue;
                if (System.Enum.TryParse(a_att, out parseAtt))
                {
                    if (int.TryParse(a_value, out a_parseValue))
                    {
                        //success
                    }
                    else
                    {
                        Debug.LogError("Parse Failed!");
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError("Parse Failed!");
                    yield break;
                }

                CardAttribute a_parsed = new CardAttribute() { attribute = parseAtt, value = a_parseValue };

                if (kw.value == 0)
                {
                    //0 is all
                    foreach (Card c in owner.hand.cards)
                    {
                        if (!c.attributes.Any(x => x.attribute == a_parsed.attribute))
                        {
                            c.attributes.Add(a_parsed);
                            c.UpdateDisplay();
                        }
                        else
                        {
                            Log.Write(c.cardName + " already has this attribute!");
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < kw.value; ++i)
                    {

                        GameManager.instance.playPile.Hide(true);
                        yield return owner.StartCoroutine(owner.StartCardSelect("Choose " + (kw.value - i) + " cards from your hand, give them " + a_parsed.attribute.ToString() + " " + a_parsed.value.ToString(), owner.hand, "Done"));
                        if (owner.selectedCard != null)
                        {
                            Log.Write("addkeyword card selected");
                            //owner.GainEnergy(owner.selectedCard.energyCost);
                            if (!owner.selectedCard.attributes.Any(x => x.attribute == a_parsed.attribute))
                            {
                                owner.selectedCard.attributes.Add(a_parsed);
                                owner.selectedCard.UpdateDisplay();
                            }
                            else
                            {
                                UIManager.Popup(owner.selectedCard.cardName + " already has this attribute!");

                            }

                            owner.selectedCard = null;
                        }

                        GameManager.instance.playPile.Hide(false);



                    }
                    owner.cardSelectingPile = new List<Pile>();
                }
                break;
            case CardKeyword.Keyword.OpponentDiscard:
                for (int i = 0; i < kw.value; ++i)
                {

                    GameManager.instance.playPile.Hide(true);
                    yield return owner.enemy.StartCoroutine(owner.enemy.StartCardSelect("Discard " + (kw.value - i) + " cards", owner.enemy.hand));
                    if (owner.enemy.selectedCard != null)
                    {
                        Log.Write("discard card selected");
                        owner.enemy.selectedCard.MoveToPile(owner.enemy.discardPile);
                        owner.enemy.selectedCard = null;
                    }
                    owner.enemy.cardSelectingPile = new List<Pile>();
                    GameManager.instance.playPile.Hide(false);



                }
                break;
            case CardKeyword.Keyword.OpponentStrength:
                owner.enemy.GainStrength(kw.value);
                break;
            case CardKeyword.Keyword.OpponentAgility:
                owner.enemy.GainAgility(kw.value);

                break;
            case CardKeyword.Keyword.OpponentStamina:
                owner.enemy.GainStamina(kw.value);

                break;
            case CardKeyword.Keyword.OpponentIntelligence:
                owner.enemy.GainIntelligence(kw.value);

                break;
            case CardKeyword.Keyword.Mill:
                yield return owner.StartCoroutine(owner.StartMillCoroutine(kw.value));

                break;
            case CardKeyword.Keyword.MillOpponent:
                yield return owner.enemy.StartCoroutine(owner.enemy.StartMillCoroutine(kw.value));

                break;
        }
        yield return GameManager.instance.medWait;
    }
}
