using System.IO;
using UnityEditor;

namespace ExampleEditor
{
    [InitializeOnLoad]
    public static class GitSync
    {
        private const string _GIT_PATH  = ".git/hooks";
        private const string _REAL_PATH = "Tools/GitHooks";

        private const string _FILE_NAME = "pre-commit";

        static GitSync()
        {
            FileInfo git_precommit  = new FileInfo($"{_GIT_PATH}/{_FILE_NAME}");
            FileInfo real_precommit = new FileInfo($"{_REAL_PATH}/{_FILE_NAME}");

            if(git_precommit.LastWriteTime >= real_precommit.LastWriteTime)
            {
                return;
            }

            real_precommit.CopyTo($"{_GIT_PATH}/{_FILE_NAME}", true);

            EditorUtility.DisplayDialog("提示", "检测到 git hook 更新内容, 已强制更新", "确定");
        }
    }
}