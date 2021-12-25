# Addressable-Example

解决 Unity Addressable 多人协作冲突问题

大致思路是，`group` 不参与版本控制，所有客户端自行生成，但是要解决一些细节上的处理问题，还有一些 `Addressable` 本身产生的一些 `bug`

当前 `Addressable` 使用的版本是 `1.18.3 cn`，其他的可能会不一致，需要自行辨别

## 自动全量生成 group

此处借助 [Addressable-Importer](https://github.com/favoyang/unity-addressable-importer) 工具完成这个部分的配置，具体使用方式请查看当前仓库的使用说明，整体非常简单

## gitignore 文件

需要屏蔽 `Build` 和 `Group`文件夹中的所有文件，此处的 `Group` 配置会使用 [Addressable-Importer](https://github.com/favoyang/unity-addressable-importer) 进行自动导入

## githook

在配置好 `gitignore` 文件后，`AddressableAssetSettings.asset` 文件就比较特殊了，需要参与版本管理，但是又不希望别人谁便乱推

此时需要自定义 `pre-commit` 规则，在 `commit` 验证阶段拒绝此次提交

> 此处方案是，当 `Unity` 进入编译时，每次都检查一下 `Tools/GitHooks/pre-commit` 和 `.git/hooks` 下的文件是否修改时间不一致，发现后就强制替换

这样做的好处是，既可以有效兼容 `cli` 命令，也可以有效在 `GUI` 客户端中对 `commit` 进行有效拒绝

但是这样又会带来一个问题，比如首次提交，或者我明确需要修改这些文件，那么需要借助 `git --no-verify` 命令来跳过检查 (也可以简写为 `git -n`)

对于使用 `GUI` 的用户来说，并不熟悉命令行，此时还需要提供一个配置文件，用于开启和关闭某些 `commit` 的规则，这时候就需要修改 `HookConfig.conf` 文件，其中 `1` 代表开启，`0` 代表关闭

- 提供的默认规则中 `HookConfig.conf` 默认禁止提交，且没有开关，必须使用 `git -n` 进行提交
- `ProjectSetting` 默认禁止提交
- `Linq` 如果代码文件中出现了 `Linq` 相关的语法，也会禁止提交
    - 当出现了此处拒绝时，会自动将 `git diff` 的结果存储在 `Temp/GitDiff.txt` 文件中，方便后续查找问题

## 源码修改

![](pic/16404103831136.jpg)


在 `AddressableAssetsSettingsGroupEditor.cs` `193` 行，增加了一个新的工具，用于强制刷新所有 `group` 中 `Missing Reference` 的问题，具体代码自行查看即可

部分情况下，会存在 `Importer` 插件重新生成或者新增 `group` 配置时，`Addressable` 无法正确找到一些资源，导致 `Group Editor` 页面一堆 `Missing Reference`，直接点击这个工具会自动清除