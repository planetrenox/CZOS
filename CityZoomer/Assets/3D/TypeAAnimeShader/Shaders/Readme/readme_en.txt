--------------------------------------------------
© 2018 Type74 nonact / InsideThighs
Version 1.0.8
web			http://type74.lsrv.jp/
mail		nonact@type74.lsrv.jp
uploader	https://ux.getuploader.com/type74/
--------------------------------------------------
!! English sentences are written using google translation.
--------------------------------------------------
Main Shader

TypeA/Toon02			Basic Shader

TypeA/Toon02Edge		A shader that added an outline to Toon02

Sub shader

TypeA/Toon02Fade		A shader that makes a part of the stencil mask translucent.

TypeA/Toon02FadeEdge	A shader that added an outline to Toon02Fade

2019 Or later
TypeA/	Toon03
		Toon03Edge
		Toon03Fade
		Toon03FadeEdge

--------------------------------------------------
Although it is a shader that started to be created for VRChat,
It can also be used for normal animation / illustration style expression purpose.

Adjust with color space "linear". (For VRChat)

Precautions when using stencil mask.
Since the drawing pass increases by one to render the translucent portion,
Fade shaders are not necessary except when you want to make a part translucent.


Points to note when using with VRChat.

In the current VRChat, the problem that render queues and render types are ignored has been resolved.
This eliminates the need to deal with multiple files as before.
I deleted the sample file for VRChat that I no longer need.

If you do not know the mask value setting please refer to the sample or check the details on the site.

Reference material for creating each texture and vcolor using blender.
http://type74.lsrv.jp/sample-using-blender/

--------------------------------------------------
The preview of the material may become a gray sphere immediately after the new import.
It seems to be a phenomenon that occurs because the project window is not updated, and it will be displayed normally when prompting to redraw.
It is displayed normally by the following things.
 ·Click play view
 ·Change parameters
 ·Material ReImport
 ·Reload project
etc.

--------------------------------------------------
--------------------------------------------------
Inspector description
--------------------------------------------------
TypeA/Toon02
--------------------------------------------------
Rendering Settings
--------------------------------------------------
Rendering Mode
    Sets the rendering mode.

RenderQueue
    Sets the RenderQueue.

SrcBlend
    Specify the blending method.
	(When the rendering mode is CutOut, Fade, Transparent.)

DstBlend
    Specify the blending method.
	(When the rendering mode is CutOut, Fade, Transparent.)

AlphaCutoff
	Set the cutoff value.
	(When the rendering mode is CutOut.)

ColorMask
    Channel settings for writing. Channels that have been checked will be written.

AlphaToMask
    Whether to mask the alpha channel. Mask if checked.

ZWrite
    Whether to write to the depth buffer or not is set.

Culling
    Specify the culling.

ZTest
	Specify the depth test method.

ObjectColor
    Specify the color of the object.
    Normally you do not need to change it from white(rgb = 1.0,1.0,1.0).
    When the rendering mode is CutOut/Fade/Transparent, alpha can be used as transparency.

--------------------------------------------------
Stencil Settings
--------------------------------------------------
ReferenceValue
    Set the value to be referenced during mask.

ReadMask
    Specify the mask value that specifies the bits affected when reading the stencil buffer.

WriteMask
    Specify the mask value that specifies the bit to be affected when writing the stencil buffer.

ComparisonFunction
    Sets how to compare the current contents of the buffer with the reference value.

PassOperation
    If you pass each test, set what to do with the contents of the buffer.

FailOperation
    If stencil test fails, set what to do with buffer contents.

ZFailOperation
    If the depth test fails, set what to do with the contents of the buffer.

--------------------------------------------------
Toon02Fade / Toon02FadeEdge
Additional setting items
--------------------------------------------------
FadeMask settings
readMask
    Set the bits to affect for fade.
    Use it when making a part translucent.

Stencil Fade
    Set the strength of the fade.

--------------------------------------------------
--------------------------------------------------
Main Color Settings
--------------------------------------------------
BaseColor
    Sets the texture of the basic color.
    If no texture is specified, the color set by the color picker will be used.

ShadowColor
    Sets the texture of the shadow color.
    If no texture is specified, the color set by the color picker will be used.

ShadowMap
    It is used to specify where you want to add shadows manually.
    Make the shadow part black, and make the bright part white.
    Results of lighting · It is combined with the value of system shadow.
    If you gradate it, the way the shadow is attached will be like semi-fixed.

Toon
    Divide it into the basic color and the shadow color with this texture.
    This texture controls how color changes.

Lighting Direction
    Specify the method of lighting.

	It becomes lighting from View direction at 0, Light direction at 1.
    You can mix the two with the slider.

    View    Those other than the system shadow will always be illuminating from the viewpoint direction.

    Light   Of all the light information available in BasePass, it is treated as having the light source in the brightest direction.
            (one per-pixel directional light and all per-vertex/SH lights.)
			Lights processed with additional pixel lighting (AddPass) are not included because they do not capture information.

			The following are included in the light source judgment.
			·Main light (brightest Directional Light)
			·Point lights shaded per vertex (for up to 4 NotImportant)
			·Ambient light obtained from LightProb. SH lights.

    In order to prevent the phenomenon that boundaries overlap by multiple lighting, it is treated as having a light source in the direction of the strongest influence.

    If set to Light and there is no information indicating the direction, it is treated as if the light source is above the camera in world space.
    If the view is mixed and there is no information indicating the direction, it is treated as the view direction (set to 0).

AnchorPoint     Added in v1.0.7
    First aid when the anchor override set on the mesh does not work.
    Specify the distance between the mesh position and the position you want to use for anchor override.
    You can check it by creating an empty object temporarily and looking at the value of the transform position after moving it on the tree.

You can adjust the shadowing by changing the normal with the following 4 settings.

Expand Spherically
    Expand the normal into a sphere. When set to 1, it will be perfectly spherical.

Spherical Center
    Designate the center position when expanding into a sphere. When set to 0, this is the center position of the mesh.
	When Mesh is divided within the same object, the mesh center can be specified by specifying the value of MeshRenderer >> Bounds >> Center.

Normal Scale X,Y,Z
    Scales vertex normals by specified coordinates.

Normal Rotation X,Y
    Rotate the vertex normals for each specified coordinate axis.



MainLighting Affect
    We will set the impact of main lighting including GI.

AmbientLight Color
    Specify whether to use the component of ambient light included in MainLightingAffcet as it is or as brightness information only.
	Keep the main lighting as it is and adjust it when you want to weaken only the color component of the ambient light.
	0 at brightness only, 1 at normal color.

AmbientTilt     Added in v1.0.7
    Adjusts the effectiveness of ambient light.
    If you have a vertically long model such as a human body model and the influence of ambient light near the upper and lower ends is too strong, you can reduce the effect by lowering the value.
    There are various factors and preferences, but in the case of the human body model, I feel that around 0.7 is reasonable.

AddLighting Affect
    If there is additional light, set the influence degree of additional light.

Saturated Value
    Adjust the "saturation value" of the whiteout by lighting.
    It becomes the setting as to how many values equal to or more than 1.0 are used.

Shadow Affect
    Set the shadow of the lighting calculation and the degree of influence of the part specified by ShadowMap.

SystemShadow Affect
    If you are using unity's system shadow, set its impact level.

Handl Shadow
	Sets whether to include the system shadow in the shadow of the diffuse.

	IncludeInDiffuese
		Combine it into the diffuse shadow.

	DropAsAShadow
		It is drawn as a shadow separately from the shadow of the diffuse, separately designating the color.

SystemShadowColor
	As with ShadowColor, you can specify the color of the system shadow with texture.
	If no texture is specified, the color set by the color picker will be used.

--------------------------------------------------
Hilight Settings
--------------------------------------------------
HilightMap
    Set the texture that specifies the area where the highlight appears and the color that blurs around it.
	If no texture is specified, the color set by the color picker will be used.

Toon(Hilight)
    Controls the color change of the highlight.

Hilight Intensity
    Set the strength in the highlight's overall.

Hilight in Shadow
    Set the highlight intensity in the shadow area.

Hilight Hardness
    Set the hardness of the highlight.

Hilight Power
    Adjust the strength of the center of the highlight.

Color Spread
    Set the strength of color blur around the highlight.
    If it is set to zero, the color of blur will not come out.
    Adjust if toon adjustment is too strong.

--------------------------------------------------
--------------------------------------------------
Outline Edge Settings
Toon02Edge / Toon02FadeEdge
--------------------------------------------------
Edge Color
    Sets the color of the outline.

BaseColorMixing
    Sets whether or not to combine outline color with basic color.

Mixing strength
    When synthesizing, set its strength.

ShadowColorMixing
    Sets whether or not to combine outline color with shadow color.

Mixing strength
    When synthesizing, set its strength.

Edge Tchickness
    Sets the thickness of the outline.

VColor(R) to Edge
    Sets whether to control the thickness of the outline by using the red component of the vertex color.
	When it is 1, the maximum thickness is obtained, and when it is zero, the outline disappears.

--------------------------------------------------
--------------------------------------------------
Version History

2021/05/07 v1.0.8
 ·Fixed the symptomatism that the thickness of the outline changed drastically due to the change of FOV.
 ·Changed the method of maintaining the thickness of the outline so that the thickness is adjusted only within a certain distance.
   I placed the T-pose model about 1.5 meters from the camera and adjusted it so that all outlines had almost the same thickness when looking at the tip of the right hand from the tip of the left hand.

2021/05/05 v1.0.7
 ·The problem that occurred when using VRChat before was solved by moving to unity 2018, and on the contrary, the thing that caused the problem was fixed.
   Fixed a bug caused by the code to deal with the display in the mirror of VRChat.
 ·Since VRChat has moved to unity 2018, problems related to render queues have been resolved and it is no longer necessary to prepare multiple files, so the sample file for VRChat has been deleted.
 ·Added two parameters.
  ·AnchorPoint
   First aid when the anchor override set on the mesh does not work.
   It seems to have no effect except those included in the FBX file to which the mesh belongs.
   (Confirmed in VRChat and unity.
  ·AmbientTilt
   For adjustment when the influence near the feet is strong in a vertically long model such as a human body model.
 ·Temporarily added a shader compatible with unity 2019.
   Since 2019, the #pragma keyword has changed.
   Therefore, shaders up to 2018 will be defective after 2019, and shaders for 2019 and later will be defective before 2018.

2019/05/01	v1.0.6
 ·Fixed the bug that some display was wrong when displaying avatar selection UI when using with VRChat.
 ·Provisional support for the problem that the outline mesh may be shifted and displayed when using VR environment with VRChat.
 ·When setting a shader created for VRChat as a use shader, changed to automatically set RenderType and RenderQueue specified in the file.
 ·Targeting "Rendering Mode", "Render Queue", "SrcBlend", "DstBlend" of RenderingSettings.
  In the case of Render Type Cutout, "ColorMask" and "AlphaToMask" are additionally targeted.
 ·The condition is that the shader name contains the required string, because it is determined by the shader name when selected.
  "vrc" "cutout" "fade" "transparent"
 ·Review and fix lighting processing other than the main light.
  In order to prevent the phenomenon that multiple painted parts overlap by multiple lightings, we will shade as the one with the light source in the brightest direction among all the light information available in BasePass.
  Lights processed with additional pixel lighting (AddPass) are not included because they do not capture information.
  If there is no directional information, it is treated as if the light source is above the camera in world space.
 ·The fixed highlight in the view direction has been changed to change to some extent depending on the lighting direction.
 ·The following five items were added and the behavior of one item was changed.
  ·Additional parameter
   ·ColorMask
    Channel settings for writing. Channels that have been checked will be written.
   ·AlphaToMask
    Whether to mask the alpha channel. Mask if checked.
   ·Expand Spherically
    Expand the normal into a sphere. When set to 1, it will be perfectly spherical.
   ·Spherical Center
    Designate the center position when expanding into a sphere. When set to 0, this is the center position of the mesh.
	When Mesh is divided within the same object, the mesh center can be specified by specifying the value of MeshRenderer >> Bounds >> Center.
   ·AmbientLight Color
    Specify whether to use the component of ambient light included in MainLightingAffcet as it is or as brightness information only.
	Keep the main lighting as it is and adjust it when you want to weaken only the color component of the ambient light.
	0 at brightness only, 1 at normal color.
  ·Changed parameter
   ·Lighting Direction
    Changed to set by slider so that "View" and "Light" can be mixed.
	0 is the previous View, 1 is the previous Light.
	Since the internal values have not changed, the previous settings are maintained.

2019/04/12	v1.0.5
 ·The highlight has been fixed to avoid the phenomenon that the area spreads extremely at a certain angle.
 ·Reduce the extra effects of system shadows that occur when setting to display the back side.
 (Because of the mechanism, for the back side can not get the correct effect, adjusted in the direction that the shadow has already fallen.)
 ·Adjust for changes in outline at medium distances.

2019/04/06	v1.0.4
 ·Fixed a bug that occurred because the extra lighting did not match the basspath lighting.
 ·Adjusted the combination of direct light and ambient light to look similar to the behavior of StandardShader.
 ·Fixed a bug where highlight was not disabled until the shader was reloaded.
 ·Adjust the appearance of the highlight color.
 ·Added adjustment parameters for system shadows of additional lights.
 ·Added two render queue change shaders required for use with VRChat.

2018/12/12	v1.0.3
 ·Adjust color appearance according to unity 2017.
 ·Added properties for adjusting the saturation value of the whiteness.

2018/12/04	v1.0.2
 ·Fixed an error in TypeAedge.cginc under certain conditions.
 ·Partial color adjustment of sample texture.
 ·Added Transparent thing to sample shader for VRC.

2018/09/28	v1.0.1
 ·Fixed a problem with the shade of the fade part when LightingDirection was Light.
 ·When RenderingMode is Cutout, the problem that there was no additional light effect in the fade part was corrected.
 ·Fixed sometimes there was a case that it was not transparent when looking at the game tab.
 ·Added basic shader which becomes base of file copy for use with VRChat.
 ·Changed a part of the description of ReadMe.
						
2018/09/24	v1.0.0
 ·First release
