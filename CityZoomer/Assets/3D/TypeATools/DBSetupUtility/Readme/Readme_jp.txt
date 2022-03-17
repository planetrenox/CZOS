--------------------------------------------------
TypeATools DynamicBone SetupUtility
--------------------------------------------------
© 2018 Type74 nonact / InsideThighs
Version 1.0.1
web	http://type74.lsrv.jp/
mail	nonact@type74.lsrv.jp
uploader	https://ux.getuploader.com/type74/
--------------------------------------------------
DynamicBoneの再設定を楽にするツールです。

DynamicBone設定完了したリファレンスからDynamicBone/DynamicBoneColliderのデータを読み込み保存。
構成が同じ別のオブジェクトに読み込んだデータを元にして設定を復元します。

元モデルなどからは切り離したものとして保存してあるので、
読み込んだデータに変更が必要無ければ元のオブジェクトは不要になります。

複数のデータをセットとして保存出来ますが、操作対象オブジェクトの名前を変更したり、
オブジェクト側の名前を変更する事でどの対象に復元するかなど自由に設定出来ます。

--------------------------------------------------
項目の説明
--------------------------------------------------
SettingsName　データ作成時の名前。

Save　全てのデータを保存します。
Create　データセットを新規作成します。
Delete　編集中のデータセットを削除します。

RootObject　シーン直下の一番親になるオブジェクトの名前。
BoneRoot　コライダーを設置するツリーの一番親になるオブジェクトの名前。
ReferenceObject　設定を読み込む為の元になるオブジェクトの一番親の名前。
DynamicBoneRoot　DynamicBoneのコンポーネントを設置するツリーの一番親になるオブジェクトの名前。

Override　DynamicBone / DinamicBoneCollider 個別で上書きするかどうか指定出来ます。
　　チェックすると上書きで設置します。
　Raplace Colliders　DynamicBoneをOverrideしない場合でもColliders情報を差し替えるかどうか指定します。
　　チェックすると、CollidersをOverrideしDynamicBoneをOverrideしない場合にCollidersの対象を新しいColliderに差し替えます。

Attach　編集中のデータのRootObjectにDynamicBone/DynamicBoneColliderを設置復元します。

Read　編集中のデータのReferenceObjectから編集中のデータ用に情報を読み込みます。

Delete　編集中のデータのRootObjectからDynamicBone/DynamicBoneColliderを全て削除します。
　削除対象となるのはBoneRootとDynamicBoneRootで指定したオブジェクトの配下のオブジェクトのみになります。
　そこから外れた位置に設置してある物はそのまま残ります。

Save Path	保存するファイルを変更する場合にここで指定します。
Load　Save Path で指定したファイルを読み込みます。
--------------------------------------------------
使い方
--------------------------------------------------
アップデート時に使用中のデータが更新されてしまうのを防ぐ為に、本使用時には以下の手順で Save Path をSampleDataから必ず変更してください。

Assets/TypeATools/DBSetupUtility/Data/SampleData にあります。

1,InspectorでSampleDataをCtlr+Dで複製しリネーム。
2,Save Pathに1で変更したものを指定。
3,Saveで保存。

ツールバーにTypeAToolsという項目が増えるのでそこからDynamicBoneSetupUtilityを選択し操作ウィンドウを開いてください。

SettingsNameとRootObjectは同じ名前で操作対象の名前にすると分かりやすいと思います。
DynamicBoneを直接ArmatureにつけるならBoneRootとDynamicBoneRootはArmatureなど同じオブジェクトを指定する事になります。

保持データは名前で判別しているので、参考画像のようにDynamicBoneを別ツリー（構成は自由）に設置する場合、
各設置オブジェクトの名前をArmature側のBoneの名前と揃える事で、読み込み元がArmatureに設置している場合でも復元可能です。
逆パターンの別ツリーから読み込み同一ツリーに復元する事も可能になります。
裏を返すと同じ名前の設置オブジェクトが無い場合復元出来ないという事になります。

各設定をしたらReadボタンで読み込み、Attachボタンで設置します。

設置先にあるDynamicBone / DynamicBoneColliderを削除したい場合、Deleteボタンで全て削除出来ます。

--------------------------------------------------
注意事項
--------------------------------------------------
プレハブの元になっているオブジェクトに対してはリファレンスとしての読み込み操作のみ可能です。
プレハブの元になっているオブジェクトに対して設置操作を行うとシーン直下にColliderオブジェクトが作成されたりコンソールに大量のエラー表示が出ます。
このオブジェクトに使用したい場合インスタンス又は複製した物に使用してください。

unityのプレハブデータ破損防止の仕組みのようでオブジェクト追加時に親指定が無効とされてしまうなどの不都合があります。
インスタンスや複製した物に操作出来れば問題ないのでそのままとします。
--------------------------------------------------
Version History

2018/10/18	v1.0.1		DynamicBoneColliderの情報が無い時の動作に不具合があったのを修正。
						保存単位の名前をRootObjectからSettingsNameに変更。
						Override時の挙動を変更。
						　Override時には一旦全て削除し、リファレンスデータに無い物が残らないようにしました。
						これに伴いDynamicBone情報をOverrideしない場合にCollidersを差し替えるかどうかの設定を追加。

2018/10/16	v1.0.0		First release
