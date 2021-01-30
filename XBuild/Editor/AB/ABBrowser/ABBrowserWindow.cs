

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using XTianGlyph;

namespace XBuild.AB.ABBrowser
{
    public class ABBrowserWindow : EditorWindow
    {
        class Styles
        {
            public static readonly GUIStyle btnInvisible = "InvisibleButton";
            public static readonly GUIContent btnSearchAB = new GUIContent("AB", "search AB");
            public static readonly GUIContent btnSearchAssets = new GUIContent("Assets", "search assets");
            public static readonly GUIContent[] searchBtns = new GUIContent[] { btnSearchAB, btnSearchAssets };
        }
        private static ABBrowserWindow s_Instance;
        internal static ABBrowserWindow Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = GetWindow<ABBrowserWindow>();
                }
                return s_Instance;
            }
        }

        [SerializeField] private TianGlyphPanel m_Panel;
        [SerializeField] private ABType m_SelectedABType;
        [SerializeField] private AssetsType m_SelectedAssetType;
        [SerializeField] private BuildTarget m_SelectedPlatform = BuildTarget.StandaloneWindows64;
        [SerializeField] private int m_SelectedPlatformIndex;
        [SerializeField] private int m_SelectedSearchIndex;

        [SerializeField] TreeViewState m_LTTreeListState;
        [SerializeField] TreeViewState m_LBTreeListState;
        [SerializeField] TreeViewState m_RTTreeListState;

        [SerializeField] MultiColumnHeaderState m_LTTreeListMCHState;
        [SerializeField] MultiColumnHeaderState m_LBTreeListMCHState;
        [SerializeField] MultiColumnHeaderState m_RTTreeListMCHState;

        private ABListTree m_ABTree;
        private ABListTree m_ABDepTree;
        private AssetListTree m_AssetTree;
        private MessageListPanel m_MessagePanel;
        const float k_ToolbarPadding = 15;
        const float k_MenubarPadding = 46.5f;
        const float kButtonWidth = 150;

        private GUIContent m_RefreshTexture;
        private Texture2D m_AndroidTexture;
        private Texture2D m_IOSTexture;
        private Texture2D m_StandaloneTexture;
        private GUIContent[] m_PlatformTextures;
        private SearchField m_SearchField;


        [MenuItem("XBuild/AB-Browser")]
        static void ShowWindow()
        {
            s_Instance = null;
            Instance.titleContent = new GUIContent("ABBrowser");
            Instance.Show();
        }

        public void SelectedABList(List<ABInfo> infoList, bool isDep)
        {
            ABDatabase.RefreshAssets(infoList);
            if (!isDep)
            {
                m_AssetTree.UpdateInfoList(null);
                m_ABDepTree.UpdateInfoList(ABDatabase.GetABDepInfoList(infoList));
            }
        }

        public void SelectedAssetsList(List<ABAssetsInfo> assetsList)
        {
            m_MessagePanel.SetSelectedAssets(assetsList);
        }

        private void OnEnable()
        {
            ABDatabase.RefreshAB(m_SelectedPlatform);
            var panelPos = GetPanelArea();
            if (m_Panel == null)
            {
                m_Panel = new TianGlyphPanel(this);
            }
            m_Panel.RTOffset = 16.5f;
            m_Panel.LBOffset = 0f;
            m_RefreshTexture = new GUIContent(EditorGUIUtility.FindTexture("Refresh"), "Refresh AB list");
            m_StandaloneTexture = EditorGUIUtility.FindTexture("BuildSettings.Standalone@2x");
            m_IOSTexture = EditorGUIUtility.FindTexture("BuildSettings.iPhone@2x");
            m_AndroidTexture = EditorGUIUtility.FindTexture("BuildSettings.Android@2x");
            m_PlatformTextures = new GUIContent[] {
                new GUIContent(m_StandaloneTexture,"PC AB"),
                new GUIContent(m_IOSTexture,"iOS AB"),
                new GUIContent(m_AndroidTexture,"Android AB")
            };
            m_SearchField = new SearchField();
        }

        private void OnDisable()
        {
        }

        private void Update()
        {
            ABDatabase.Update();
            if (m_ABTree != null)
            {
                if (ABDatabase.IsABDirty())
                {
                    m_ABTree.UpdateInfoList(ABDatabase.GetABInfoList(m_SelectedABType));
                }
                if (ABDatabase.IsAssetDirty())
                {
                    m_AssetTree.UpdateInfoList(ABDatabase.GetAssetInfoList(m_SelectedAssetType));
                }
            }
        }

        private void OnGUI()
        {
            var panelRect = GetPanelArea();
            InitPanelTree();
            GUIButton();
            m_Panel.OnGUI(panelRect);

            GUISearchAndAsset();
        }

        private void GUIButton()
        {
            GUILayout.BeginHorizontal();
            var btnHeight = 27f;
            var platform = GUILayout.SelectionGrid(m_SelectedPlatformIndex, m_PlatformTextures, 3,
                GUILayout.Width(btnHeight * 3.3f), GUILayout.Height(btnHeight));
            if (platform != m_SelectedPlatformIndex)
            {
                m_SelectedPlatformIndex = platform;
                switch (m_SelectedPlatformIndex)
                {
                    case 0: m_SelectedPlatform = BuildTarget.StandaloneWindows64; break;
                    case 1: m_SelectedPlatform = BuildTarget.iOS; break;
                    case 2: m_SelectedPlatform = BuildTarget.Android; break;
                }
                ABDatabase.RefreshAB(m_SelectedPlatform);
            }
            var clicked = GUILayout.Button(m_RefreshTexture, GUILayout.Width(btnHeight), GUILayout.Height(btnHeight));
            if (clicked)
            {
                ABDatabase.RefreshAB(m_SelectedPlatform);
            }
            var tabLabels = new string[]{
                "All - " + ABDatabase.GetABTypeSizeStr(ABType.All),
                "Model - "+ ABDatabase.GetABTypeSizeStr(ABType.Model),
                "Scene - "+ ABDatabase.GetABTypeSizeStr(ABType.Scene),
                "UI - "+ ABDatabase.GetABTypeSizeStr(ABType.UI),
                "Other - "+ ABDatabase.GetABTypeSizeStr(ABType.Other),
                "Dep - "+ ABDatabase.GetABTypeSizeStr(ABType.Dep)
            };
            var barWidth = position.width - 5 * btnHeight + 6;
            var selected = (ABType)GUILayout.Toolbar((int)m_SelectedABType, tabLabels, GUILayout.Height(btnHeight),
                GUILayout.Width(barWidth));
            if (selected != m_SelectedABType)
            {
                m_SelectedABType = selected;
                m_ABTree.UpdateInfoList(ABDatabase.GetABInfoList(m_SelectedABType));
            }
            GUILayout.EndHorizontal();
        }

        private void GUISearchAndAsset()
        {
            var searchHeight = 16.8f;
            var btnWidth = 98;
            m_SelectedSearchIndex = GUILayout.SelectionGrid(m_SelectedSearchIndex, Styles.searchBtns, 2,
                GUILayout.Width(btnWidth), GUILayout.Height(searchHeight - 2));
            var rect = new Rect(m_Panel.LTRect.x + btnWidth, m_Panel.LTRect.y - searchHeight + 0.5f,
                m_Panel.LTRect.width - btnWidth + 2 * m_Panel.SplitterWidth, searchHeight);
            if (m_SelectedSearchIndex == 0)
            {
                var newSearch = m_SearchField.OnGUI(rect, ABDatabase.abSearchString);
                if (ABDatabase.abSearchString != newSearch)
                {
                    ABDatabase.abSearchString = newSearch;
                    m_ABTree.UpdateInfoList(ABDatabase.GetABInfoList(m_SelectedABType));
                    Repaint();
                }
            }
            else
            {
                var newSearch = m_SearchField.OnGUI(rect, ABDatabase.assetsSearchString);
                if (ABDatabase.assetsSearchString != newSearch)
                {
                    ABDatabase.assetsSearchString = newSearch;
                    m_AssetTree.UpdateInfoList(ABDatabase.GetAssetInfoList(m_SelectedAssetType));
                    Repaint();
                }
            }
            var tabLabels = new string[]{
                "All\n" + ABDatabase.GetAssetTypeSizeStr(AssetsType.None),
                "Material\n"+ ABDatabase.GetAssetTypeSizeStr(AssetsType.Material),
                "Texture\n"+ ABDatabase.GetAssetTypeSizeStr(AssetsType.Texture),
                "Prefab\n"+ ABDatabase.GetAssetTypeSizeStr(AssetsType.Prefab),
                "Shader\n"+ ABDatabase.GetAssetTypeSizeStr(AssetsType.Shader),
                "Asset\n"+ ABDatabase.GetAssetTypeSizeStr(AssetsType.Asset),
                "Other\n"+ ABDatabase.GetAssetTypeSizeStr(AssetsType.Other),
            };
            var tabHeight = 34;
            var barWidth = m_Panel.RTRect.width;
            var barRect = new Rect(m_Panel.RTRect.x, m_Panel.RTRect.y - tabHeight,
                m_Panel.RTRect.width, tabHeight - 0.5f);
            var selected = (AssetsType)GUI.Toolbar(barRect, (int)m_SelectedAssetType, tabLabels);
            if (selected != m_SelectedAssetType)
            {
                m_SelectedAssetType = selected;
                m_AssetTree.UpdateInfoList(ABDatabase.GetAssetInfoList(m_SelectedAssetType));
            }
        }

        private void InitPanelTree()
        {
            if (m_ABTree == null)
            {
                if (m_LTTreeListState == null)
                    m_LTTreeListState = new TreeViewState();

                var headerState = ABListTree.CreateDefaultMultiColumnHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_LTTreeListMCHState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_LTTreeListMCHState, headerState);
                m_LTTreeListMCHState = headerState;
                m_ABTree = new ABListTree(m_LTTreeListState, m_LTTreeListMCHState, false);
                m_Panel.LTOutline = false;
                m_Panel.LTPanel = m_ABTree;
                m_Panel.LTPanel.Reload();
            }
            if (m_ABDepTree == null)
            {
                if (m_LBTreeListState == null)
                    m_LBTreeListState = new TreeViewState();

                var headerState = ABListTree.CreateDefaultMultiColumnHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_LBTreeListMCHState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_LBTreeListMCHState, headerState);
                m_LBTreeListMCHState = headerState;

                m_ABDepTree = new ABListTree(m_LBTreeListState, m_LBTreeListMCHState, true);
                m_Panel.LBOutline = false;
                m_Panel.LBPanel = m_ABDepTree;
                m_Panel.LBPanel.Reload();
            }

            if (m_Panel.RTPanel == null)
            {
                if (m_RTTreeListState == null)
                    m_RTTreeListState = new TreeViewState();

                var headerState = AssetListTree.CreateDefaultMultiColumnHeaderState();
                if (MultiColumnHeaderState.CanOverwriteSerializedFields(m_RTTreeListMCHState, headerState))
                    MultiColumnHeaderState.OverwriteSerializedFields(m_RTTreeListMCHState, headerState);
                m_RTTreeListMCHState = headerState;

                m_AssetTree = new AssetListTree(m_RTTreeListState, m_RTTreeListMCHState);
                m_Panel.RTOutline = false;
                m_Panel.RTPanel = m_AssetTree;
                m_Panel.RTPanel.Reload();
            }
            if (m_Panel.RBPanel == null)
            {
                m_MessagePanel = new MessageListPanel();
                m_Panel.RBPanel = m_MessagePanel;
                m_Panel.RBOutline = true;
                m_Panel.RBPanel.Reload();
            }
        }

        private Rect GetPanelArea()
        {
            return new Rect(0, k_MenubarPadding, position.width, position.height - k_MenubarPadding);
        }
    }
}