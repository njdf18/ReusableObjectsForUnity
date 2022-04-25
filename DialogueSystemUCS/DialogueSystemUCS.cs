using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Maybe I will use new InputSystem but not now.
/// Input.GetKeyDown(KeyCode.Space) = @event.IsActionPressed("ui_accept")
/// </summary>
public class DialogueSystemUCS : MonoBehaviour
{
    public class Event   // Can be loaded when every scene is ready
    {
        public List<Command> Commands;
        public Dictionary<string, int> Labels = new Dictionary<string, int>();
        public bool IsPreprocessed;

        public void EventPreprocess()
        {
            for (int i = 0; i < Commands.Count; i++)
            {
                if (Commands[i].Code == CommandType.Label)
                {
                    Labels.Add(Commands[i].Paras[0].ToString(), i);
                    Commands.Remove(Commands[i]);
                }
            }
        }
    }
    public struct Command
    {
        public CommandType Code;
        public object[] Paras;
        public ValueTypeGroup ValueTypeGroup;

        public Command(CommandType code, object[] paras = null, ValueTypeGroup valueTypeGroup = default)
        {
            Code = code;
            Paras = paras;
            ValueTypeGroup = valueTypeGroup;
        }
    }
    public struct ValueTypeGroup
    {
        public int[] Ints;
        public float[] Floats;
        public bool[] Booleans;
        public Position[] Positions;
        public SoundManager.SoundChannel[] SoundChannels;
        public Color[] Colors;
    }

    public enum CommandType
    {
        Log,
        Text,
        Choice,
        If,
        Switch,
        Goto,
        Label,
        BoxState,
        Wait,
        Image,
        Tachie,
        Flash,
        SoundPlay,
        SoundStop,
        CameraMove,
        CameraShake,
        CameraStatic,
        MapsceneLoad,
        ReadModeStart,
        ReadModeText,
        ReadModeClear,
        ReadModeEnd,
        Action,
        Timer,
        TimerStop,
    }
    public enum Position { Left, Centre, Right }

    private readonly int[] _nameBoxPosX = new int[] { 152, 960, 1768 };
    private readonly float[] _displaySpeedConst = new float[] { 0.01f, 0.05f, 0.1f };

    #region Components
    [SerializeField] private Image _nameBox, _waitIcon;
    [SerializeField] private TextMeshProUGUI _name, _sentence, _readModeSentence, _timerText;
    [SerializeField] private Animation _animPlyr, _iconAnimPlyr;
    [SerializeField] private RawImage _mask, _flash, _readModeBG, _choiceMask;
    [SerializeField] private GameObject _selectionsGridLayer, _tachieLayer, _imageLayer;
    private Button[] _selections;
    private TextMeshProUGUI[] _selectionsTexts = new TextMeshProUGUI[5];
    #endregion

    public static DialogueSystemUCS Ins;
    [HideInInspector] public bool IsExecuting;
    [HideInInspector] public int ChosenIndex;

    private bool _isCountingDown, _isChosen;
    private int _min, _sec, _ms100;
    private Command _nowCommand;
    private List<Action> _customActions = new List<Action>();
    private List<Func<bool>> _customConditionMethod = new List<Func<bool>>();
    private List<Func<string>> _customSwitchMethod = new List<Func<string>>();

    public delegate void CurrentEventCompleted();
    public event CurrentEventCompleted CurrentEventCompletedHandler;
    public delegate void CountDownTimeout();
    public event CountDownTimeout CountDownTimeoutHandler;
    private delegate void ChoiceSelected();
    private event ChoiceSelected ChoiceSelectedHandler;

    void Awake()
    {
        if (Ins == null)
        {
            Ins = this;
            DontDestroyOnLoad(Ins);
        }
        else
            Destroy(this);
    }
    void Start()
    {
        _selections = _selectionsGridLayer.GetComponentsInChildren<Button>();
        for (int i = 0; i < _selections.Length; i++)
        {
            _selectionsTexts[i] = _selections[i].GetComponentInChildren<TextMeshProUGUI>();
            _selections[i].gameObject.SetActive(false);
        }
    }

    #region Public DialogueSystem Command
    public static Command LogMes(params object[] message)
    {
        return new Command(CommandType.Log, message);
    }
    public static Command Wait(float seconds)
    {
        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.Floats = new float[] { seconds };

        return new Command(CommandType.Wait, null, valueTypeGroup);
    }
    public static Command Text(string nameText, string sentenceText, bool skippable, Position nameBoxPos = Position.Left)
    {
        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.Positions = new Position[] { nameBoxPos };
        valueTypeGroup.Booleans = new bool[] { skippable };

        return new Command(CommandType.Text, new object[] { nameText, sentenceText }, valueTypeGroup);
    }
    public static Command Choice(string contents)
    {
        return new Command(CommandType.Choice, new object[] { contents.Split(',') });
    }
    public static Command Label(string labelName)
    {
        return new Command(CommandType.Label, new object[] { labelName });
    }
    public static Command If(Func<bool> conditionMethod, string jumpToLabelWhenFalse)
    {
        Ins._customConditionMethod.Add(conditionMethod);

        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.Ints = new int[] { Ins._customConditionMethod.Count - 1 };

        return new Command(CommandType.If, new object[] { jumpToLabelWhenFalse }, valueTypeGroup);
    }
    public static Command Switch(Func<string> switchMethod)
    {
        Ins._customSwitchMethod.Add(switchMethod);

        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.Ints = new int[] { Ins._customSwitchMethod.Count - 1 };

        return new Command(CommandType.Switch, null, valueTypeGroup);
    }
    public static Command Goto(string labelName)
    {
        return new Command(CommandType.Goto, new object[] { labelName });
    }
    public static Command BoxState(bool state)
    {
        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.Booleans = new bool[] { state };

        return new Command(CommandType.BoxState, null, valueTypeGroup);
    }
    public static Command SoundPlay(SoundManager.SoundChannel channel, AudioClip audio, float pitch = 1f)
    {
        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.SoundChannels = new SoundManager.SoundChannel[] { channel };
        valueTypeGroup.Floats = new float[] { pitch };

        return new Command(CommandType.SoundPlay, new object[] { audio }, valueTypeGroup);
    }
    public static Command SoundStop(SoundManager.SoundChannel channel, float duration)
    {
        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.SoundChannels = new SoundManager.SoundChannel[] { channel };
        valueTypeGroup.Floats = new float[] { duration };

        return new Command(CommandType.SoundStop, null, valueTypeGroup);
    }
    public static Command Image(int layer, Texture image, float duration, Color initial, Color final)
    {
        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.Ints = new int[] { layer };
        valueTypeGroup.Floats = new float[] { duration };
        valueTypeGroup.Colors = new Color[] { initial, final };

        return new Command(CommandType.Image, new object[] { image }, valueTypeGroup);
    }
    public static Command Tachie(Position pos, Texture image, float duration, Color initial, Color final)
    {
        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.Positions = new Position[] { pos };
        valueTypeGroup.Floats = new float[] { duration };
        valueTypeGroup.Colors = new Color[] { initial, final };

        return new Command(CommandType.Tachie, new object[] { image });
    }
    public static Command Flash(Color color, float duration, float waitDuration)
    {
        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.Floats = new float[] { duration, waitDuration };
        valueTypeGroup.Colors = new Color[] { color };

        return new Command(CommandType.Flash, null, valueTypeGroup);
    }
    // public static Command CameraMove(Vector2 pos, float duration, bool needIndexCalculate = true)
    // {
    //     return new Command(CommandType.CameraMove, new object[] { pos, duration, needIndexCalculate });
    // }
    // public static Command CameraShake(float amplitude, float duration = 0f)
    // {
    //     return new Command(CommandType.CameraShake, new object[] { amplitude, duration });
    // }
    // public static Command CameraStatic()
    // {
    //     return new Command(CommandType.CameraStatic);
    // }
    public static Command MapsceneLoad(string MapName)
    {
        return new Command(CommandType.MapsceneLoad, new object[] { MapName });
    }
    public static Command ReadModeStart()
    {
        return new Command(CommandType.ReadModeStart);
    }
    public static Command ReadModeText(string text)
    {
        return new Command(CommandType.ReadModeText, new object[] { text });
    }
    public static Command ReadModeClear()
    {
        return new Command(CommandType.ReadModeClear);
    }
    public static Command ReadModeEnd()
    {
        return new Command(CommandType.ReadModeEnd);
    }
    public static Command Action(Action action, Event @event)
    {
        Ins._customActions.Add(action);

        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.Ints = new int[] { Ins._customActions.Count - 1 };

        return new Command(CommandType.Action, null, valueTypeGroup);
    }
    public static Command Timer(int min, int sec, int ms100)
    {
        ValueTypeGroup valueTypeGroup = new ValueTypeGroup();
        valueTypeGroup.Ints = new int[] { min, sec, ms100 };

        return new Command(CommandType.Timer, null, valueTypeGroup);
    }
    public static Command TimerStop()
    {
        return new Command(CommandType.TimerStop);
    }
    #endregion
    public void Execute(Event @event)
    {
        StartCoroutine(C_Execute(@event));
    }
    private IEnumerator C_Execute(Event @event)
    {
        if (@event.Commands.Count == 0)
        {
            Debug.LogError("You didn't register any command");
            yield break;
        }

        if (IsExecuting)
        {
            Debug.LogWarning("Previous event is executing.");
            yield break;
        }

        if (!@event.IsPreprocessed)
        {
            @event.EventPreprocess();
            @event.IsPreprocessed = true;
        }

        //Avoid interacting with any control here
        _mask.raycastTarget = true;   // Control.MouseFilterEnum.Stop
        Cursor.visible = false;
        IsExecuting = true;

        //Vector2 CameraOriginalPos = new Vector2();    For CameraShake
        int lastReadModeStrNum = 0;
        for (int i = 0; i < @event.Commands.Count; i++)
        {
            _nowCommand = @event.Commands[i];

            switch (_nowCommand.Code)
            {
                case CommandType.Log:
                    Debug.Log(_nowCommand.Paras[0]);
                    break;
                case CommandType.Text:
                    {
                        string nameText = _nowCommand.Paras[0].ToString();
                        if (!nameText.Equals("System"))
                        {
                            SetVisible(_nameBox, true);
                            _nameBox.rectTransform.anchoredPosition = new Vector2(_nameBoxPosX[(int)_nowCommand.ValueTypeGroup.Positions[0]], -656);
                            _name.text = nameText;
                        }
                        else
                            SetVisible(_nameBox, false);

                        _sentence.maxVisibleCharacters = 0;
                        _sentence.text = (string)_nowCommand.Paras[1];
                        bool skippable = _nowCommand.ValueTypeGroup.Booleans[0];

                        WaitForSeconds displaySpeedWait = new WaitForSeconds(_displaySpeedConst[1]);
                        for (int j = 1; j < _sentence.text.Length; j++)
                        {
                            if (Input.GetKeyDown(KeyCode.Space) && skippable)
                                break;
                            _sentence.maxVisibleCharacters = j;
                            yield return displaySpeedWait;
                        }
                        _sentence.maxVisibleCharacters = _sentence.text.Length;
                        WaitIconAnim(true);
                        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
                        WaitIconAnim(false);
                        break;
                    }
                case CommandType.Choice:
                    {
                        Cursor.visible = true;

                        var content = (string[])_nowCommand.Paras[0];
                        for (int j = 0; j < content.Length; j++)
                        {
                            _selectionsTexts[j].text = content[j];
                            _selections[j].gameObject.SetActive(true);
                        }

                        _animPlyr.Play("ChoiceBoxOpen");
                        yield return new WaitUntil(() => !_animPlyr.IsPlaying("ChoiceBoxOpen"));

                        yield return new WaitUntil(() => _isChosen);
                        _isChosen = false;

                        _animPlyr.Play("ChoiceBoxClose");
                        yield return new WaitUntil(() => !_animPlyr.IsPlaying("ChoiceBoxClose"));

                        for (int j = 0; j < content.Length; j++)
                            _selections[j].gameObject.SetActive(false);

                        Cursor.visible = false;
                        break;
                    }
                case CommandType.If:
                    {
                        bool result = Ins._customConditionMethod[_nowCommand.ValueTypeGroup.Ints[0]]();
                        if (!result)
                            i = @event.Labels[_nowCommand.Paras[0].ToString()] - 1;
                        break;
                    }
                case CommandType.Switch:
                    {
                        string result = Ins._customSwitchMethod[_nowCommand.ValueTypeGroup.Ints[0]]();
                        i = @event.Labels[result] - 1;
                        break;
                    }
                case CommandType.Goto:
                    {
                        i = @event.Labels[_nowCommand.Paras[0].ToString()] - 1;
                        break;
                    }
                case CommandType.BoxState:
                    {
                        string animName = string.Empty;
                        if (_nowCommand.ValueTypeGroup.Booleans[0])  // state
                            animName = "SentenceBoxOpen";
                        else
                        {
                            SetVisible(_nameBox, false);
                            animName = "SentenceBoxClose";
                        }
                        _animPlyr.Play(animName);
                        yield return new WaitUntil(() => !_animPlyr.IsPlaying(animName));

                        _sentence.text = string.Empty;
                        break;
                    }
                case CommandType.Wait:
                    {
                        yield return new WaitForSecondsRealtime(_nowCommand.ValueTypeGroup.Floats[0]);
                        break;
                    }
                case CommandType.Image:
                    {
                        Image image = _imageLayer.transform.GetChild(_nowCommand.ValueTypeGroup.Ints[0]).GetComponent<Image>();
                        if (_nowCommand.Paras[1] != null)
                            image.sprite = (Sprite)_nowCommand.Paras[0];

                        StartCoroutine(
                            ImageColorTween(
                                image,
                                _nowCommand.ValueTypeGroup.Colors[0],
                                _nowCommand.ValueTypeGroup.Colors[1],
                                _nowCommand.ValueTypeGroup.Floats[0]
                                )
                        );
                        break;
                    }
                case CommandType.Tachie:
                    {
                        int pos = (int)_nowCommand.ValueTypeGroup.Positions[0];
                        Image tachie = _tachieLayer.transform.GetChild(pos).GetComponent<Image>();
                        if (_nowCommand.Paras[0] != null)
                            tachie.sprite = (Sprite)_nowCommand.Paras[0];

                        StartCoroutine(
                            ImageColorTween(
                                tachie,
                                _nowCommand.ValueTypeGroup.Colors[0],
                                _nowCommand.ValueTypeGroup.Colors[1],
                                _nowCommand.ValueTypeGroup.Floats[0]
                                )
                        );
                        break;
                    }
                case CommandType.Flash:
                    {
                        float timeElapsed = 0f, halfDuration = _nowCommand.ValueTypeGroup.Floats[0] / 2;
                        while (timeElapsed < halfDuration)
                        {
                            _flash.color = Color.Lerp(_flash.color, _nowCommand.ValueTypeGroup.Colors[0], timeElapsed / halfDuration);
                            timeElapsed += Time.deltaTime;
                            yield return null;
                        }
                        _flash.color = _nowCommand.ValueTypeGroup.Colors[0];


                        float waitSec = _nowCommand.ValueTypeGroup.Floats[1];
                        if (waitSec > 0)
                        {
                            yield return new WaitForSecondsRealtime(waitSec);
                        }

                        timeElapsed = 0f;
                        while (timeElapsed < halfDuration)
                        {
                            _flash.color = Color.Lerp(_flash.color, new Color(0, 0, 0, 0), timeElapsed / halfDuration);
                            timeElapsed += Time.deltaTime;
                            yield return null;
                        }
                        break;
                    }
                case CommandType.SoundPlay:
                    {
                        SoundManager.Ins.Play(
                            _nowCommand.ValueTypeGroup.SoundChannels[0],
                            (AudioClip)_nowCommand.Paras[0],
                            _nowCommand.ValueTypeGroup.Floats[0]
                        );
                        break;
                    }
                case CommandType.SoundStop:
                    {
                        SoundManager.Ins.AudioFadeOut(
                            _nowCommand.ValueTypeGroup.SoundChannels[0],
                            _nowCommand.ValueTypeGroup.Floats[0]
                        );
                        break;
                    }
                #region inherited from last project: CameraMove, CameraShake, and CameraStatic
                // case CommandType.CameraMove:
                //     {
                //         _otherTween.InterpolateProperty(MapsceneCamera, "position", null, _nowCommand.Paras[0], (float)_nowCommand.Paras[1], Tween.TransitionType.Sine);
                //         _otherTween.Start();
                //         Vector2 nextPos = (Vector2)_nowCommand.Paras[0];
                //         if ((bool)_nowCommand.Paras[2])    // needIndexCalculate
                //         {
                //             MapsceneManager.index = (int)Math.Abs(nextPos.x / GlobalDataManager.Width);
                //             this.EmitSignal(nameof(MapPersIndexChanged), MapsceneManager.index);
                //         }
                //         await ToSignal(_otherTween, "finished");
                //         break;
                //     }
                // case CommandType.CameraShake:
                //     {
                //         GlobalDataManager.Ins.CameraShake_Amplitude = (float)_nowCommand.Paras[0];
                //         float sec = (float)_nowCommand.Paras[1];
                //         CameraOriginalPos = MapsceneCamera.Position;
                //         GlobalDataManager.Ins.CameraShake_isShaking = true;
                //         if (sec == Single.PositiveInfinity)
                //             GlobalDataManager.Ins.CameraShake_isShaking = true;
                //         else if (sec == 0f)
                //         {
                //             _waitTimer.WaitTime = 0.7f;
                //             _waitTimer.Start();
                //             await ToSignal(_waitTimer, "timeout");
                //             GlobalDataManager.Ins.CameraShake_isShaking = false;
                //             MapsceneCamera.Position = CameraOriginalPos;
                //             tachieLayer.Position = Vector2.Zero;
                //         }
                //         else  //sec > 0
                //         {
                //             _waitTimer.WaitTime = sec;
                //             _waitTimer.Start();
                //             await ToSignal(_waitTimer, "timeout");
                //             GlobalDataManager.Ins.CameraShake_isShaking = false;
                //             MapsceneCamera.Position = CameraOriginalPos;
                //             tachieLayer.Position = Vector2.Zero;
                //         }
                //         break;
                //     }
                // case CommandType.CameraStatic:
                //     {
                //         GlobalDataManager.Ins.CameraShake_isShaking = false;
                //         MapsceneCamera.Position = CameraOriginalPos;
                //         tachieLayer.Position = Vector2.Zero;
                //         break;
                //     }
                #endregion
                case CommandType.ReadModeStart:
                    {
                        SetVisible(_readModeBG, true);
                        _animPlyr.Play("ReadModeEnter");

                        yield return new WaitUntil(() => !_animPlyr.IsPlaying("ReadModeEnter"));
                        break;
                    }
                case CommandType.ReadModeText:
                    {
                        _readModeSentence.maxVisibleCharacters = lastReadModeStrNum;
                        var newText = ((string)_nowCommand.Paras[0]).Replace("[下一行]", "\n");
                        _readModeSentence.text += newText;

                        WaitForSeconds displaySpeedWait = new WaitForSeconds(_displaySpeedConst[0]);
                        for (int j = lastReadModeStrNum; j < lastReadModeStrNum + newText.Length; j++)
                        {
                            if (Input.GetKeyDown(KeyCode.Space))
                                break;
                            _readModeSentence.maxVisibleCharacters = j;
                            yield return displaySpeedWait;
                        }
                        lastReadModeStrNum += newText.Length;
                        _readModeSentence.maxVisibleCharacters = lastReadModeStrNum;

                        WaitIconAnim(true);
                        yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Space));
                        WaitIconAnim(false);
                        break;
                    }
                case CommandType.ReadModeClear:
                    {
                        WaitIconAnim(false);
                        lastReadModeStrNum = 0;
                        _animPlyr.Play("ReadModeSentenceClean");

                        yield return new WaitUntil(() => !_animPlyr.IsPlaying("ReadModeSentenceClean"));

                        _readModeSentence.text = string.Empty;
                        _animPlyr.Play("ReadModeSentenceEnter");

                        yield return new WaitUntil(() => !_animPlyr.IsPlaying("ReadModeSentenceClean"));
                        break;
                    }
                case CommandType.ReadModeEnd:
                    {
                        WaitIconAnim(false);
                        lastReadModeStrNum = 0;
                        _animPlyr.Play("ReadModeExit");

                        yield return new WaitUntil(() => !_animPlyr.IsPlaying("ReadModeExit"));

                        SetVisible(_readModeBG, false);
                        break;
                    }
                case CommandType.Action:
                    {
                        Ins._customActions[_nowCommand.ValueTypeGroup.Ints[0]]();  //actionIndex
                        break;
                    }
                case CommandType.Timer:
                    {
                        _min = _nowCommand.ValueTypeGroup.Ints[0];
                        _sec = _nowCommand.ValueTypeGroup.Ints[1];
                        _ms100 = _nowCommand.ValueTypeGroup.Ints[2];
                        // mm:ss:[ms][ms]
                        _animPlyr.Play("TimerTextEnter");

                        _timerText.text = "[center]" + _min.ToString("00") + "：" + _sec.ToString("00") + "：" + _ms100.ToString("00");
                        _isCountingDown = true;
                        StartCoroutine(CountDown());
                        break;
                    }
                case CommandType.TimerStop:
                    {
                        _isCountingDown = false;
                        break;
                    }
            }
        }

        IsExecuting = false;
        _mask.raycastTarget = false;  // Control.MouseFilterEnum.Ignore;
        Cursor.visible = true;

        if (CurrentEventCompletedHandler != null)
            CurrentEventCompletedHandler();
    }

    private void SetVisible(MaskableGraphic uiObject, bool state)
    {
        int stateInteger = state ? 1 : 0;
        uiObject.rectTransform.localScale = new Vector2(stateInteger, stateInteger);
    }
    private void SetVisible(Button uiObject, bool state)
    {
        int stateInteger = state ? 1 : 0;
        uiObject.transform.localPosition = new Vector2(stateInteger, stateInteger);
    }
    private void WaitIconAnim(bool play)
    {
        SetVisible(_waitIcon, play);
        if (play)
            _iconAnimPlyr.Play("WaitIcon_Wait");
        else
            _iconAnimPlyr.Stop();
    }
    private IEnumerator ImageColorTween(Graphic image, Color firstValue, Color finalValue, float duration)
    {
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            image.color = Color.Lerp(firstValue, finalValue, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        image.color = finalValue;
    }
    private IEnumerator CountDown()
    {
        WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.001f);
        while ((_min > 0 || _sec > 0 || _ms100 > 0) && _isCountingDown)
        {
            _ms100--;
            if (_ms100 <= 0 && _sec > 0)
            {
                _sec--;
                _ms100 = 9;
            }

            if (_sec <= 0 && _min > 0)
            {
                _min--;
                _sec = 59;
            }

            _timerText.text = $"{_min.ToString("00")}：{_sec.ToString("00")}：{_ms100.ToString("00")}";
            yield return wait;
        }

        if (CountDownTimeoutHandler != null)
            CountDownTimeoutHandler();

        _isCountingDown = false;
        _animPlyr.Play("TimerTextExit");
    }

    public void OnButtonClick(int chosenIndex)
    {
        _isChosen = true;
        ChosenIndex = chosenIndex;
    }
}
