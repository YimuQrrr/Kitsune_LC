using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using MelonLoader;
using UnityEngine;
using GameNetcodeStuff;
using UnityEngine.InputSystem;
using TMPro;
using SharpOSC;
using System.Reflection;
using static UnityEngine.InputSystem.InputRemoting;


namespace CL_TOOL_DLL
{
    public class Kitsune_LC : MelonMod
    {
        // 场景加载时
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            log("OnSceneWasLoaded");
            OSC_Initialize();
        }

        // 场景卸时
        public override void OnSceneWasUnloaded(int buildIndex, string sceneName)
        {
            log("OnSceneWasUnloaded");
            OSC_Initialize();
        }

        // 场景初始化时
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            log("OnSceneWasInitialized");
            OSC_Initialize();
            //如果在场景里
            bool flag = sceneName == "SampleSceneRelay";
            if (flag)
            {
                log("in SampleSceneRelay");
                this.inGame = true;
                this.inMenu = false;
                this.Initialize();
            }
            //如果在主菜单
            bool flag2 = sceneName == "MainMenu";
            if (flag2)
            {
                log("in MainMenu");
                this.inGame = false;
                this.inMenu = true;
                this.Initialize();
            }
        }
        //初始化
        public void Initialize()
        {
            log("Initialize");
            if (this.inMenu)
            {
                GameObject LANWarning = GameObject.Find("LANWarning");
                if (LANWarning != null)
                {
                    log("Found: LANWarning");
                    this.LANWarning = LANWarning;
                }
                else
                {
                    log("LANWarning object not found");
                }

                GameObject MenuNotification = GameObject.Find("MenuNotification");
                if (MenuNotification != null)
                {
                    log("Found: MenuNotification");
                    this.MenuNotification = MenuNotification;
                }
                else
                {
                    log("MenuNotification object not found");
                }

                if (this.MenuNotification != null)
                {
                    MenuNotification.SetActive(false);
                }

                if (this.LANWarning != null)
                {
                    LANWarning.SetActive(false);
                }
            }
            else if (this.inGame)
            {
                //游戏组件初始化
                GameObject PlayerControl_Object = GameObject.Find("Player");
                if (PlayerControl_Object != null)
                {
                    log("Found: Player");
                    this.PlayerCL = PlayerControl_Object.GetComponent<PlayerControllerB>();
                }
                else
                {
                    log("Player object not found");
                }

                GameObject playerHUDHelmetModel = GameObject.Find("PlayerHUDHelmetModel");
                if (playerHUDHelmetModel != null)
                {
                    log("Found: PlayerHUDHelmetModel");
                    this.playerHUDHelmetModel = playerHUDHelmetModel;
                }
                else
                {
                    log("PlayerHUDHelmetModel object not found");
                }

                GameObject Terminal_Object = UnityEngine.Object.FindAnyObjectByType<Terminal>()?.gameObject;
                if (Terminal_Object != null)
                {
                    log("Found: Terminal");
                    this.terminal = Terminal_Object.GetComponent<Terminal>();
                }
                else
                {
                    log("Terminal object not found");
                }

                if (this.PlayerCL != null && this.playerHUDHelmetModel != null && this.terminal != null)
                {
                    playerHUDHelmetModel.SetActive(false);
                    this.initialized = true;
                }
                else
                {
                    log("Initialization failed due to missing components");
                }
            }
            else
            {
                return;
            }

            
        }

        //MOD加载成功时
        public override void OnInitializeMelon()
        {
            string asciiArt = @"
       _ _                            __    ___ 
  /\ /(_) |_ ___ _   _ _ __   ___    / /   / __\
 / //_/ | __/ __| | | | '_ \ / _ \  / /   / /   
/ __ \| | |_\__ \ |_| | | | |  __/ / /___/ /___ 
\/  \/|_|\__|___/\__,_|_| |_|\___| \____/\____/ 
                                                

>>> Type /help in game chat box to start
";
            
            log(asciiArt);

        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //每帧执行
        public override void OnUpdate()
        {
            
            bool flag = this.inGame && this.initialized;    //初始化成功和在游戏中
            if (flag)
            {
                // 每帧查找 chatText
                if (chatText == null)
                {
                    GameObject chatTextObject = GameObject.Find("ChatText");
                    if (chatTextObject != null)
                    {
                        log("Found: " + chatTextObject.name);
                        chatText = chatTextObject.GetComponent<TMP_Text>();
                        if (chatText != null)
                        {
                            log("Found: Text");
                        }
                        else
                        {
                            log("Text Miss");
                        }
                    }
                }
                else
                {
                    // 处理文本框最新的一行
                    string[] lines = chatText.text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length > 0)
                    {
                        string lastLine = lines[lines.Length - 1].Trim();
                        if (lastLine != lastProcessedLine)
                        {
                            log($"ChatText: {lastLine}");
                            lastProcessedLine = lastLine;
                            ProcessChatText(lastLine);
                        }
                    }
                }
                //更新参数状态
                //体力
                if (this.infiniteSprint_Enabled) 
                {
                    this.PlayerCL.isSprinting = false;

                }
                //电力
                if (this.infiniteBattery_Enabled)
                {
                    bool isHeldObject = this.PlayerCL.currentlyHeldObjectServer != null; //如果手里有东西
                    if (isHeldObject)
                    {
                        bool isServer = this.PlayerCL.IsServer; //受否是房主
                        if (isServer)
                        {
                            this.PlayerCL.currentlyHeldObjectServer.insertedBattery.charge = 1f;
                        }
                        else
                        {
                            this.PlayerCL.currentlyHeldObject.insertedBattery.charge = 1f;
                        }
                    }
                    
                }
                if (PlayerCL.health < 100)
                {
                    // 受到伤害
                    if (PlayerCL.health < previousHealth)
                    {
                        harm = previousHealth - PlayerCL.health;    //计算伤害
                        OSC_intensity(harm);
                        OSC_shock(PlayerCL.health);
                        OSC_shock(1);
                        previousHealth = PlayerCL.health;
                    }
                    else
                    {
                        previousHealth = PlayerCL.health;
                    }
                    //如果在当前值死
                    if (PlayerCL.isPlayerDead)
                    {
                        PlayerCL.health = 0;
                    }
                    
                }
                else
                {
                    previousHealth = PlayerCL.health;
                }
                // 直接触发死的方法
                if (PlayerCL.health == 100 && PlayerCL.isPlayerDead)
                {
                    PlayerCL.health = 0;
                }

                if (this.TEST_Enabled)
                {

                }


            }
        }
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        // 处理文本框内容
        private void ProcessChatText(string text)
        {
            if (text.Contains("/help")) { ShowHelp(); }

            else if (text.Contains("/Helmet on")) { HELMET_ON(); }
            else if (text.Contains("/Helmet off")) { HELMET_OFF(); }

            else if (text.Contains("/iSprint on")) { infiniteSprint_ON(); }
            else if (text.Contains("/iSprint off")) { infiniteSprint_OFF(); }

            else if (text.Contains("/iBattery on")) { infiniteBattery_ON(); }
            else if (text.Contains("/iBattery off")) { infiniteBattery_OFF(); }

            else if (text.Contains("/iSpeed on")) { SpeedCheating_ON(); }
            else if (text.Contains("/iSpeed off")) { SpeedCheating_OFF(); }
            else if (text.Contains("/test")) { TEST(); }




        }
        // 开启 PlayerHUDHelmetModel
        private void HELMET_ON()
        {
            if (playerHUDHelmetModel != null)
            {
                playerHUDHelmetModel.SetActive(true);
                chatText.text += "\n" + "<color=#00FF00>PlayerHUDHelmetModel is ON</color>\n";
                log("PlayerHUDHelmetModel ON");
            }
        }

        // 关闭 PlayerHUDHelmetModel
        private void HELMET_OFF()
        {
            if (playerHUDHelmetModel != null)
            {
                playerHUDHelmetModel.SetActive(false);
                chatText.text += "\n" + "<color=#00FF00>PlayerHUDHelmetModel is OFF</color>\n";
                log("PlayerHUDHelmetModel OFF");
            }
        }

        
        //无限体力/开
        private void infiniteSprint_ON()
        {
            log("in infiniteSprint_ON");
            chatText.text += "\n" + "<color=#00FF00>infiniteSprint is ON</color>\n";
            this.infiniteSprint_Enabled = true;

        }
        //无限体力/关
        private void infiniteSprint_OFF()
        {
            log("in infiniteSprint_OFF");
            chatText.text += "\n" + "<color=#00FF00>infiniteSprint is OFF</color>\n";
            this.infiniteSprint_Enabled = false;
        }
        //无限电池/开
        private void infiniteBattery_ON()
        {
            log("infiniteBattery is ON");
            chatText.text += "\n" + "<color=#00FF00>infiniteBattery is ON</color>\n";
            this.infiniteBattery_Enabled = true;
        }
        //无限电池/关
        private void infiniteBattery_OFF()
        {
            log("infiniteBattery is OFF");
            chatText.text += "\n" + "<color=#00FF00>infiniteBattery is OFF</color>\n";
            this.infiniteBattery_Enabled = false;
        }

        //快速移动/开
        private void SpeedCheating_ON()
        {
            if (PlayerCL != null)
            {
                PlayerCL.isSpeedCheating = true;
                chatText.text += "\n" + "<color=#00FF00>SpeedCheating is ON</color>\n";
                log("SpeedCheating is ON");
            }
        }
        //快速移动/关
        private void SpeedCheating_OFF()
        {
            if (PlayerCL != null)
            {
                PlayerCL.isSpeedCheating = false;
                chatText.text += "\n" + "<color=#00FF00>SpeedCheating is OFF</color>\n";
                log("SpeedCheating is OFF");
            }
        }

        private void TEST()
        {
            log("in TEST");
            this.TEST_Enabled = !this.TEST_Enabled;
        }


        //OSC
        private void OSC_shock(float osc_shock)
        {
            var sender = new UDPSender("127.0.0.1", 9001);
            var Msg_OSC_shock = new OscMessage("/Lethal_Company_shock", osc_shock);

            sender.Send(Msg_OSC_shock);
            log("[OSCsend] /Lethal_Company_shock:"+ osc_shock);
        }

        private void OSC_intensity(float osc_intensity)
        {
            var sender = new UDPSender("127.0.0.1", 9001);
            var Msg_OSC_shock = new OscMessage("/Lethal_Company_intensity", osc_intensity);

            sender.Send(Msg_OSC_shock);
            log("[OSCsend] /Lethal_Company_intensity:" + osc_intensity);

        }



        //help
        private void ShowHelp()
        {
            if (chatText != null)
            {
                string helpMessage = "<color=#8030ff>Kitsune_LC v0.2.1</color>" +
                                     "<color=#FFFF00>by </color>" +
                                     "<color=#fe7701>YimuQr</color>\n" +

                                     "<color=#00FF00>Available commands:</color>\n" +
                                     "<color=#FFFF00>/Helmet  on/off</color>- Turn on/off the helmet model\n" +
                                     "<color=#FFFF00>/iSprint  on/off</color> - Turn on/off infiniteSprint\n" +
                                     "<color=#FFFF00>/iBattery  on/off</color> - Turn on/off infiniteBattery\n" +
                                     "<color=#FFFF00>/iSpeed  on/off</color> - Turn on/off SpeedCheating\n" +
                                        
                                     "<color=#FFFF00>/test</color> - test the switch\n";
                chatText.text += "\n" + helpMessage;
            }
        }
        //OSC初始化
        private void OSC_Initialize()
        {
            OSC_intensity(1);
            OSC_shock(1);
            log("OSC Initialize");
        }

        //控制台日志
        private void log(string msg)
        {
            MelonLogger.Msg(">> "+ msg);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //游戏场景状态
        private bool inGame = false;
        private bool inMenu = false;

        //初始化状态
        private bool initialized = false;

        private bool infiniteSprint_Enabled = false;

        private bool infiniteBattery_Enabled = false;

        private bool TEST_Enabled = false;

        //玩家参数控制
        private PlayerControllerB PlayerCL = null;


        //用于存储聊天框上一次处理的行
        private string lastProcessedLine = "";

  

    
        //游戏组件

        //头盔
        private GameObject playerHUDHelmetModel = null;
        //烦人的警告UI
        private GameObject MenuNotification = null;
        private GameObject LANWarning = null;

        //控制台
        private Terminal terminal = null;
        //聊天框
        private TMP_Text chatText = null;
        //OSC
     
        //当前血量
        private float previousHealth = 100;
        //受到的伤害
        private float harm = 0;
        //TEST
        

        //开关

    }
}
