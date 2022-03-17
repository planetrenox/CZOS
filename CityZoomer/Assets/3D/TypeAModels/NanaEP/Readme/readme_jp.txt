--------------------------------------------------
TypeA Model Nana-EP
--------------------------------------------------
© 2018 Type74 nonact / InsideThighs
Version 1.0.2
web	http://type74.lsrv.jp/
mail	nonact@type74.lsrv.jp
uploader	https://ux.getuploader.com/type74/
--------------------------------------------------
VRChatでTypeA AnimeShaderを利用しての使用を想定して制作されているモデルです。
VRChatのポリゴン数制限が２万から７万に引き上げられたので、アニメ調のNanaをリメイクしイラスト調に仕上げたものです。

シェーダーはTypeA AnimeShaderか同等機能のあるものを利用して下さい。

Mecanim humanoid対応なのでVRChat以外の用途にも利用可能です。


当モデルは以下の条件でセットアップしてあります。

使用アセット
TypeA AnimeShader
Dynamic Bone
プロジェクトセッティング
> Player > Other Settings > Color Space > Linear
> Project Settings > Quality > Anti Aliasing > 8x Multi Sampling
--------------------------------------------------
--------------------------------------------------
セット内容
--------------------------------------------------
TypeA Model Nana-EP Total 25118~31743
	nana_cos01_EP.fbx / 25033
	nana_cos07_EP.fbx / 18408
	nana_hair_F_EP.fbx / 1954
	nana_hair_R_Pony_EP.fbx / 4756

BlendShape
	VRC LipSync用 x15
	VRCアイトラッキング対応自動瞬き用(打ち消し) x4
	MMDの標準的な表情に対応(2バイト文字名) x36
	その他(アルファベット表記名) x41
	サンプルフェイシャルアニメーション(.anim) x19

VRChat Custom Overrideで使用可能なモーション x10
	Idle_EP
	Walk_Fwd / Walk_Back / Walk_RT45 / Walk_LT45 / Walk_RT135 / WalkLT135
	Run_Fwd / Run_RT45 / Run_LT45
	PronIdle / PronFwd

マテリアル・揺れ物はセットアップ済みですが対応アセットが無い場合各自対応してください。

--------------------------------------------------
その他
--------------------------------------------------
各prefabのBodyにはBody_blinkという名前のAnimatorが、自動瞬き用にセットしてあります。

FacialSamleのsceneでは、再生してAnimatorWindowでnana-EP_FacialTestのanimatorのParametersを変更すると表情が切り替わります。

アニメタイプの目3種(>< / oo / ==)は、メッシュが3つ分埋め込んであるので同時に3種類設定出来ます。
VRChatなど表情変更時にマテリアルも変更可能な場合、別途マテリアルを用意すれば表情を増やせます。

--------------------------------------------------
--------------------------------------------------
利用規約
--------------------------------------------------
著作権は作者であるInsideThighs(=nonact)が保有しています。

UnityAssetstore利用規約に従って下さい。

更に以下の件を追加します。

1, VRChatのVRC_Avatar Pedestalなど、購入者以外の人が利用可能になる仕組みへの利用は禁止します。
 オリジナルゲームのキャラクターとして利用する場合などはこれに含まれません。
2, 当データ又は改変したものを使用したことにより発生したトラブルや損失に関して作者は一切の責任を負いません。
--------------------------------------------------
Version History

2019/04/06
v1.0.2
　・RenderQueueによる透過物に発生していた不具合回避の為、対象マテリアルの設定を変更しました。
　TypeAAnimeShader V1.0.4以降が必要になります。
　・TypeAAnimeShader V1.0.4に合わせて各マテリアルの設定を調整しました。

2019/01/23
v1.0.1
　・VRChat Custom Overrideで使用可能なモーションを2個追加しました。
　・スカートに余計なウェイトが効いていたのを修正しました。
　・スカート用のDynamicBoneを調整しました。

2019/01/19
v1.0.0
　・First release
