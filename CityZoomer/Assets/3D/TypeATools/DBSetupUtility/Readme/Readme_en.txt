--------------------------------------------------
TypeATools DynamicBone SetupUtility
--------------------------------------------------
© 2018 Type74 nonact / InsideThighs
Version 1.0.0
web	http://type74.lsrv.jp/
mail	nonact@type74.lsrv.jp
uploader	https://ux.getuploader.com/type74/
--------------------------------------------------
It is a tool to ease reconfiguration of DynamicBone.

Read and save the data of DynamicBone / DynamicBoneCollider from the completed reference.

Restore the settings based on the data loaded in another object.

--------------------------------------------------
description
--------------------------------------------------
SettingsName　Name when creating the data.

Save　Save all data.
Create　Create a new data set.
Delete　Delete the data set being edited.

RootObject  /  The name of the object that is the most parent immediately under the scene.
BoneRoot  /  The name of the object that will become the parent of the tree where the collider is installed.
ReferenceObject  /  The name of the parent of the object from which to read the settings.
DynamicBoneRoot  /  Name of the object that is the parent of the tree where DynamicBone components are installed.

Override　DynamicBone / DinamicBoneCollider  /  You can specify whether to overwrite individually.
  If checked it will attach with overwriting.

  　Raplace Colliders / Specify whether to replace Colliders information even if you do not override DynamicBone.
　　 If checked, even if you do not override the DynamicBone, replace Colliders' target with the new Collider.

Attach  /  Attach DynamicBone / DynamicBoneCollider to RootObject of data being edited.

Read  /  Reads information from the ReferenceObject of the data being edited.

Delete  /  Delete all DynamicBone / DynamicBoneCollider from RootObject of data being edited.
  Only objects under the object specified by BoneRoot and DynamicBoneRoot will be deleted.

--------------------------------------------------
How to use
--------------------------------------------------
Please change Save Path from SampleData in the following procedure to prevent updating the data being used at the time of updating.

Assets/TypeATools/DBSetupUtility/Data/SampleData

1, Rename by duplicating SampleData with Inspector.
2, specify the one changed by 1 to Save Path.
3, save.

Since there are more items TypeATools in the toolbar, please select DynamicBoneSetupUtility from there and open the operation window.

After making each setting, read with the Read button, and install with the Attach button.

If you want to delete the DynamicBone / DynamicBoneCollider in the installation location, you can delete all with the Delete button.

--------------------------------------------------
Notes
--------------------------------------------------
For objects that are the basis of prefabs, only read operations as references are possible.

If you want to use this object, please use it for instance or duplicate.
--------------------------------------------------
Version History

2018/10/18	v1.0.1		Fixed a problem with operation when there was no information of DynamicBoneCollider.
						
						Change the name of the storage unit from RootObject to SettingsName.
						
						Change override behavior.
						 When Override, we changed to delete things not in reference data.
						
						In accordance with this, setting whether to replace Colliders when not overriding DynamicBone information has been added.


2018/10/16	v1.0.0		First release
