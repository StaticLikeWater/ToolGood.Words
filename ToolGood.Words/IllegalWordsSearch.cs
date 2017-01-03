﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ToolGood.Words
{
    public class IllegalWordsSearchResult
    {
        internal IllegalWordsSearchResult(string keyword, int start, int end, string srcText)
        {
            Keyword = keyword;
            Success = true;
            End = end;
            Start = start;
            SrcString = srcText.Substring(Start, end - Start + 1);
        }

        private IllegalWordsSearchResult()
        {
            Success = false;
            Start = 0;
            End = 0;
            SrcString = null;
            Keyword = null;
        }
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; private set; }
        /// <summary>
        /// 开始位置
        /// </summary>
        public int Start { get; private set; }
        /// <summary>
        /// 结束位置
        /// </summary>
        public int End { get; private set; }
        /// <summary>
        /// 原始文本
        /// </summary>
        public string SrcString { get; private set; }
        /// <summary>
        /// 关键字
        /// </summary>
        public string Keyword { get; private set; }

        public static IllegalWordsSearchResult Empty { get { return new IllegalWordsSearchResult(); } }

        private int _hash = -1;
        public override int GetHashCode()
        {
            if (_hash == -1) {
                var i = Start << 5;
                i += End - Start;
                _hash = i << 1 + (Success ? 1 : 0);
            }
            return _hash;
        }
        public override string ToString()
        {
            return Start.ToString() + "|" + SrcString;
        }
    }


    public class IllegalWordsSearch
    {
        #region class
        class TreeNode
        {
            #region Constructor & Methods

            public TreeNode(TreeNode parent, char c)
            {
                _char = c; _parent = parent;
                _results = new List<string>();

                _transitionsAr = new List<TreeNode>();
                _transHash = new Dictionary<char, TreeNode>();
            }

            public void AddResult(string result)
            {
                if (_results.Contains(result)) return;
                _results.Add(result);
            }

            public void AddTransition(TreeNode node)
            {
                _transHash.Add(node.Char, node);
                _transitionsAr.Add(node);
            }

            public TreeNode GetTransition(char c)
            {
                TreeNode tn;
                if (_transHash.TryGetValue(c, out tn)) { return tn; }
                return null;
            }

            public bool ContainsTransition(char c)
            {
                return _transHash.ContainsKey(c);
            }
            #endregion

            #region Properties
            private char _char;
            private TreeNode _parent;
            private TreeNode _failure;
            private List<string> _results;
            private List<TreeNode> _transitionsAr;
            private Dictionary<char, TreeNode> _transHash;

            public char Char
            {
                get { return _char; }
            }

            public TreeNode Parent
            {
                get { return _parent; }
            }


            /// <summary>
            /// Failure function - descendant node
            /// </summary>
            public TreeNode Failure
            {
                get { return _failure; }
                set { _failure = value; }
            }


            /// <summary>
            /// Transition function - list of descendant nodes
            /// </summary>
            public List<TreeNode> Transitions
            {
                get { return _transitionsAr; }
            }


            /// <summary>
            /// Returns list of patterns ending by this letter
            /// </summary>
            public List<string> Results
            {
                get { return _results; }
            }

            #endregion
        }
        class TrieNode
        {
            //public byte Type { get; set; }
            public bool End { get; set; }
            public HashSet<string> Results { get; set; }
            private Dictionary<char, TrieNode> m_values;
            private uint minflag = uint.MaxValue;
            private uint maxflag = uint.MinValue;

            public TrieNode()
            {
                m_values = new Dictionary<char, TrieNode>();
                Results = new HashSet<string>();
            }

            public bool TryGetValue(char c, out TrieNode node)
            {
                if (minflag <= (uint)c && maxflag >= (uint)c) {
                    return m_values.TryGetValue(c, out node);
                }
                node = null;
                return false;
            }

            public void Add(TreeNode t, TrieNode node)
            {
                var c = t.Char;
                if (m_values.ContainsKey(c) == false) {
                    if (minflag > c) { minflag = c; }
                    if (maxflag < c) { maxflag = c; }
                    m_values.Add(c, node);
                    foreach (var item in t.Results) {
                        node.End = true;
                        node.Results.Add(item);
                    }
                }
            }

            public TrieNode[] ToArray()
            {
                TrieNode[] first = new TrieNode[char.MaxValue + 1];
                foreach (var item in m_values) {
                    first[item.Key] = item.Value;
                }
                return first;
            }
        }
        class NodeInfo
        {
            public int Index;
            public bool End;
            public NodeInfo Parent;
            public TrieNode Node;
            public char Type;
            public bool TryGetValue(char c, out TrieNode node)
            {
                return Node.TryGetValue(c, out node);
            }
            public bool CanJump(char c, int index, int jump)
            {
                if (Index >= index - jump) {
                    int t;
                    if (c < 127) {
                        t = Type + bitType[c];
                    } else if (c >= 0x4e00 && c <= 0x9fa5) {
                        t = Type + 'z';
                    } else {
                        return false;
                    }
                    if (t == 98 || t == 194 || t == 244) {
                        return false;
                    }
                    return true;
                }
                return false;
            }
            public NodeInfo(int index, char c, TrieNode node, NodeInfo parent = null)
            {
                Index = index;
                Node = node;
                End = node.End;
                Parent = parent;

                if (c < 127) {
                    Type = bitType[c];
                } else if (c >= 0x4e00 && c <= 0x9fa5) {
                    Type = 'z';
                } else {
                    Type = '0';
                }

            }
        }
        class SearchHelper:IDisposable
        {
            NodeInfo mainNode;
            List<NodeInfo> nodes = new List<NodeInfo>();
            private TrieNode[] _first;
            private int _jumpLength;

            public SearchHelper(ref TrieNode[] first, int jumpLength)
            {
                _first = first;
                _jumpLength = jumpLength;
            }

            public void Dispose()
            {
                mainNode = null;
                nodes.Clear();
            }

            public bool FindChar(char c, int index)
            {
                bool hasEnd = false;
                List<NodeInfo> new_node = new List<NodeInfo>();
                #region mainNode
                if (mainNode == null) {
                    TrieNode tn = _first[c];
                    if (tn != null) {
                        mainNode = new NodeInfo(index, c, tn);
                        if (mainNode.End) {
                            hasEnd = true;
                        }
                    }
                } else {
                    if (mainNode.CanJump(c, index, _jumpLength)) {
                        new_node.Add(mainNode);
                    }
                    TrieNode tn;
                    if (mainNode.TryGetValue(c, out tn)) {
                        mainNode = new NodeInfo(index, c, tn, mainNode);
                        if (mainNode.End) {
                            hasEnd = true;
                        }
                    } else {
                        tn = _first[c];
                        if (tn != null) {
                            mainNode = new NodeInfo(index, c, tn);
                            if (mainNode.End) {
                                hasEnd = true;
                            }
                        } else {
                        
                            mainNode = null;
                        }
                    }
                }
                #endregion

                foreach (var n in nodes) {
                    if (n.CanJump(c, index, _jumpLength)) {
                        new_node.Add(n);
                    }
                    TrieNode tn;
                    if (n.TryGetValue(c, out tn)) {
                        var n2 = new NodeInfo(index, c, tn, n);
                        new_node.Add(n2);

                        if (n2.End) {
                            hasEnd = true;
                        }
                    }
                }
                nodes = new_node;
                return hasEnd;
            }

            public List<Tuple<int, int, string>> GetKeywords()
            {
                List<Tuple<int, int, string>> list = new List<Tuple<int, int, string>>();

                if (mainNode!=null) {
                    if (mainNode.End) {
                        foreach (var keywords in mainNode.Node.Results) {
                            var p = mainNode;
                            for (int i = 0; i < keywords.Length - 1; i++) {
                                p = p.Parent;
                            }
                            list.Add(Tuple.Create(p.Index, mainNode.Index, keywords));
                        }
                    }
                }
             
                foreach (var node in nodes) {
                    foreach (var keywords in node.Node.Results) {
                        var p = node;
                        for (int i = 0; i < keywords.Length - 1; i++) {
                            p = p.Parent;
                        }
                        list.Add(Tuple.Create(p.Index, node.Index, keywords));
                    }
                }
                return list;
            }
        }
        #endregion

        #region Local fields
        const string bitType = "00000000000000000000000000000000000000000000000011111111110000000aaaaaaaaaaaaaaaaaaaaaaaaaa000000aaaaaaaaaaaaaaaaaaaaaaaaaa00000";

        private TrieNode _root = new TrieNode();
        private TrieNode[] _first = new TrieNode[char.MaxValue + 1];
        private int _jumpLength;
        #endregion

        public IllegalWordsSearch(int jumpLength = 1)
        {
            _jumpLength = jumpLength;
        }

        #region SetKeywords
        public void SetKeywords(ICollection<string> _keywords)
        {
            HashSet<string> list = new HashSet<string>();
            foreach (var item in _keywords) {
                list.Add(WordsHelper.ToSenseIllegalWords(item));
                var c = WordsHelper.ToSenseIllegalWords(item);
            }
            list.Remove("");
            var tn = BuildTreeWithBFS(list);
            SimplifyTree(tn);
        }
        TreeNode BuildTreeWithBFS(ICollection<string> _keywords)
        {
            var root = new TreeNode(null, ' ');
            foreach (string p in _keywords) {
                string t = p;
                //if (_quick) {
                //    t = p.ToLower();
                //} else {
                //    t = WordHelper.ToSenseWord(p);
                //}

                // add pattern to tree
                TreeNode nd = root;
                foreach (char c in t) {
                    TreeNode ndNew = null;
                    foreach (TreeNode trans in nd.Transitions)
                        if (trans.Char == c) { ndNew = trans; break; }

                    if (ndNew == null) {
                        ndNew = new TreeNode(nd, c);
                        nd.AddTransition(ndNew);
                    }
                    nd = ndNew;
                }
                nd.AddResult(t);
            }

            List<TreeNode> nodes = new List<TreeNode>();
            // Find failure functions
            //ArrayList nodes = new ArrayList();
            // level 1 nodes - fail to root node
            foreach (TreeNode nd in root.Transitions) {
                nd.Failure = root;
                foreach (TreeNode trans in nd.Transitions) nodes.Add(trans);
            }
            // other nodes - using BFS
            while (nodes.Count != 0) {
                List<TreeNode> newNodes = new List<TreeNode>();

                //ArrayList newNodes = new ArrayList();
                foreach (TreeNode nd in nodes) {
                    TreeNode r = nd.Parent.Failure;
                    char c = nd.Char;

                    while (r != null && !r.ContainsTransition(c)) r = r.Failure;
                    if (r == null)
                        nd.Failure = root;
                    else {
                        nd.Failure = r.GetTransition(c);
                        foreach (string result in nd.Failure.Results)
                            nd.AddResult(result);
                    }

                    // add child nodes to BFS list 
                    foreach (TreeNode child in nd.Transitions)
                        newNodes.Add(child);
                }
                nodes = newNodes;
            }
            root.Failure = root;
            return root;
        }
        void SimplifyTree(TreeNode tn)
        {
            _root = new TrieNode();
            Dictionary<TreeNode, TrieNode> dict = new Dictionary<TreeNode, TrieNode>();

            List<TreeNode> list = new List<TreeNode>();
            foreach (var item in tn.Transitions) list.Add(item);

            while (list.Count > 0) {
                foreach (var item in list) {
                    simplifyNode(item, tn, dict);
                }
                List<TreeNode> newNodes = new List<TreeNode>();
                foreach (var item in list) {
                    foreach (var node in item.Transitions) {
                        newNodes.Add(node);
                    }
                }
                list = newNodes;
            }
            addNode(tn, tn, _root, dict);
            _first = _root.ToArray();
        }
        void addNode(TreeNode treeNode, TreeNode root, TrieNode tridNode, Dictionary<TreeNode, TrieNode> dict)
        {
            foreach (var item in treeNode.Transitions) {
                var node = dict[item];
                tridNode.Add(item, node);
                addNode(item, root, node, dict);
            }
            if (treeNode != root) {
                var topNode = root.GetTransition(treeNode.Char);
                if (topNode != null) {
                    foreach (var item in topNode.Transitions) {
                        var node = dict[item];
                        tridNode.Add(item, node);
                    }
                }
            }
        }

        void simplifyNode(TreeNode treeNode, TreeNode root, Dictionary<TreeNode, TrieNode> dict)
        {
            List<TreeNode> list = new List<TreeNode>();
            var tn = treeNode;
            while (tn != root) {
                list.Add(tn);
                tn = tn.Failure;
            }

            TrieNode node = new TrieNode();

            foreach (var item in list) {
                if (dict.ContainsKey(item) == false) {
                    if (item.Results.Count > 0) {
                        dict[item] = new TrieNode();
                    } else {
                        dict[item] = node;
                    }
                }
            }
        }
        #endregion

        public bool ContainsAny(string text)
        {
            StringBuilder sb = new StringBuilder(text);
            TrieNode ptr = null;
            for (int i = 0; i < text.Length; i++) {
                char c;
                if (ToSenseWords(text[i], out c)) {
                    sb[i] = c;
                }
                TrieNode tn;
                if (ptr == null) {
                    tn = _first[c];
                } else {
                    if (ptr.TryGetValue(c, out tn) == false) {
                        tn = _first[c];
                    }
                }
                if (tn != null) {
                    if (tn.End) {
                        foreach (var find in tn.Results) {
                            var r = GetIllegalResult(find, c, i + 1 - find.Length, i, text, sb);
                            if (r != null) return true;
                        }
                    }
                }
                ptr = tn;
            }

            //StringBuilder sb = new StringBuilder(text);
            SearchHelper sh = new SearchHelper(ref _first, _jumpLength);

            //var searchText = WordsHelper.ToSenseWords(text);
            for (int i = 0; i < text.Length; i++) {
                char c = sb[i];
                //if (ToSenseWords(text[i], out c)) {
                //    sb[i] = c;
                //}
                if (sh.FindChar(c, i)) {
                    foreach (var keywordInfos in sh.GetKeywords()) {
                        var r = GetIllegalResult(keywordInfos.Item3, c, keywordInfos.Item1, keywordInfos.Item2, text, sb);
                        if (r != null) return true;
                    }
                }
            }
            return false;
        }

        public IllegalWordsSearchResult FindFirst(string text)
        {
            StringBuilder sb = new StringBuilder(text);
            TrieNode ptr = null;
            for (int i = 0; i < text.Length; i++) {
                char c;
                if (ToSenseWords(text[i], out c)) {
                    sb[i] = c;
                }
                TrieNode tn;
                if (ptr == null) {
                    tn = _first[c];
                } else {
                    if (ptr.TryGetValue(c, out tn) == false) {
                        tn = _first[c];
                    }
                }
                if (tn != null) {
                    if (tn.End) {
                        foreach (var find in tn.Results) {
                            var r = GetIllegalResult(find, c, i+1-find.Length, i, text, sb);
                            if (r != null) return r;
                        }
                    }
                }
                ptr = tn;
            }

            SearchHelper sh = new SearchHelper(ref _first, _jumpLength);

            for (int i = 0; i < text.Length; i++) {
                char c;
                if (ToSenseWords(text[i], out c)) {
                    sb[i] = c;
                }
                if (sh.FindChar(c, i)) {
                    foreach (var keywordInfos in sh.GetKeywords()) {
                        var r = GetIllegalResult(keywordInfos.Item3, c, keywordInfos.Item1, keywordInfos.Item2, text, sb);
                        if (r != null) return r;
                    }
                }
            }
            return IllegalWordsSearchResult.Empty;
        }

        public List<IllegalWordsSearchResult> FindAll(string text)
        {
            StringBuilder sb = new StringBuilder(text);
            SearchHelper sh = new SearchHelper(ref _first, _jumpLength);
            List<IllegalWordsSearchResult> result = new List<IllegalWordsSearchResult>();

            for (int i = 0; i < text.Length; i++) {
                char c;
                if (ToSenseWords(text[i], out c)) {
                    sb[i] = c;
                }
                if (sh.FindChar(c, i)) {
                    foreach (var keywordInfos in sh.GetKeywords()) {
                        var r = GetIllegalResult(keywordInfos.Item3, c, keywordInfos.Item1, keywordInfos.Item2, text, sb);
                        if (r != null) result.Add(r);
                    }
                }
            }

            return result;
        }

        public string Replace(string text, char replaceChar = '*')
        {
            var all = FindAll(text);
            StringBuilder result = new StringBuilder(text);
            all = all.OrderBy(q => q.Start).ThenBy(q => q.End).ToList();
            for (int i = all.Count - 1; i >= 1; i--) {
                var r = all[i];
                for (int j = r.Start; j <= r.End; j++) {
                    if (result[j]!=replaceChar) {
                        result[j] = replaceChar;
                    }
                }
            }
            return result.ToString();
        }



        #region private
        private bool isInEnglishOrInNumber(string keyword, char ch, int end, string searchText)
        {
            if (end < searchText.Length - 1) {
                if (ch < 127) {
                    var c = searchText[end + 1];
                    if (c < 127) {
                        int d = bitType[c] + bitType[ch];
                        if (d == 98 || d == 194) {
                            return true;
                        }
                    }
                }
            }
            var start = end + 1 - keyword.Length;
            if (start > 0) {
                var c = searchText[start - 1];
                if (c < 127) {
                    var k = keyword[0];
                    if (k < 127) {
                        int d = bitType[c] + bitType[k];
                        if (d == 98 || d == 194) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private IllegalWordsSearchResult GetIllegalResult(string keyword, char ch, int start, int end, string srcText, StringBuilder searchText)
        {
            if (end < searchText.Length - 1) {
                if (ch < 127) {
                    char c;
                    ToSenseWords(searchText[end + 1], out c);
                    if (c < 127) {
                        int d = bitType[c] + bitType[ch];
                        if (d == 98 || d == 194) {
                            return null;
                        }

                    }
                }
            }
            if (start > 0) {
                var c = searchText[start - 1];
                if (c < 127) {
                    var k = keyword[0];
                    if (k < 127) {
                        int d = bitType[c] + bitType[k];
                        if (d == 98 || d == 194) {
                            return null;
                        }
                    }
                }
            }
            return new IllegalWordsSearchResult(keyword, start, end, srcText);
        }

        internal bool ToSenseWords(char c, out char w)
        {
            if (c < 'A') { } else if (c <= 'Z') {
                w = (char)(c | 0x20);
                return true;
            } else if (c < 9450) { } else if (c <= 12840) {//处理数字 
                var index = Dict.nums1.IndexOf(c);
                if (index > -1) {
                    w = Dict.nums2[index];
                    return true;
                }
            } else if (c == 12288) {
                w = ' ';
                return true;

            } else if (c < 0x4e00) { } else if (c <= 0x9fa5) {
                var k = Dict.Simplified[c - 0x4e00];
                if (k != c) {
                    w = k;
                    return true;

                }
            } else if (c < 65280) { } else if (c < 65375) {
                var k = (c - 65248);
                if ('A' <= k && k <= 'Z') {
                    k = k | 0x20;
                }
                w = (char)k;
                return true;
            }
            w = c;
            return false;
        }

        #endregion



    }
}