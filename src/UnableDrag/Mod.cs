using System;
using System.Collections.Generic;
using Modding;
using Modding.Blocks;
using Modding.Levels;
using ModIO;
using UnityEngine;
using Localisation;

namespace UDspace
{
	public class Mod : ModEntryPoint
	{
		public static bool isJapanese = SingleInstance<LocalisationManager>.Instance.currLangName.Contains("���{��");
		public static GameObject mod;
		public static MessageType TranslateType; // �I�u�W�F�N�g�̈ړ����N���C�A���g�ɓ`�B
		public static readonly bool debug = false; // �J���������J�ł�
		public override void OnLoad()
		{
			mod = new GameObject("UDmod");
			SingleInstance<AddScriptManager>.Instance.transform.parent = mod.transform;
			SingleInstance<SkyboxController>.Instance.transform.parent = mod.transform;
			UnityEngine.Object.DontDestroyOnLoad(mod);

			// �I�u�W�F�N�g�̈ړ����N���C�A���g�ɓ`�B
			TranslateType = ModNetworking.CreateMessageType(DataType.Entity, DataType.Vector3, DataType.Vector3);
			ModNetworking.Callbacks[TranslateType] += new Action<Message>((msg) =>
			{
				// �N���C�A���g�̌����ڂ�����ύX				
				Entity entity = (Entity)msg.GetData(0);
				entity = entity.SimEntity;
				Vector3 pos = (Vector3)(msg.GetData(1));
				Vector3 rot = (Vector3)(msg.GetData(2));
				entity.GameObject.transform.position = pos;
				entity.GameObject.transform.rotation = Quaternion.Euler(rot);
			});
			Log("Load");
		}
        public override void OnEntityPrefabCreation(int entityId, GameObject prefab)
        {
            switch (entityId)
            {
				case 10: // SphericalGravity
					prefab.AddComponent<SphericalGravity>();
					break;
				case 11: // PlanarGravity
					prefab.AddComponent<PlanarGravity>();
					break;
				case 12: // ColonyCore
					prefab.AddComponent<ColonyCore>();
					break;
            }
        }
        public static void Log(string msg)
        {
			Debug.Log("UniverseMod : " + msg);
        }
		public static void Warning(string msg)
        {
			Debug.LogWarning("UniverseMod : " + msg);
        }
		public static void Error(string msg)
        {
			Debug.LogError("UniverseMod : " + msg);
        }
	}
	public class AddScriptManager : SingleInstance<AddScriptManager>
	{
		public override string Name { get { return "Add Script Manager"; } }
		/// <summary>
		/// ��]��R�������ɂ��邩�ǂ���
		/// </summary>
		public bool isUnableAxialDrag
		{
			set; get;
		}
		public bool isFirstFrame = true;
		public PlayerMachineInfo PMI;
		public void Awake()
		{
			Events.OnBlockInit += new Action<Block>(AddScript);
			Events.OnEntityPlaced += new Action<Entity>(AddScript);
			isUnableAxialDrag = false;
		}
		public void Update()
		{
			/*
			if (machine == null)
			{
				machine = Machine.Active(); //�厖
			}
			*/
			/*
			//�ڂ����͂��܂��낳��Ƃ̂肳��̃��v������
			if (isUnableAxialDrag)
			{
				
				if (!StatMaster.isMP && Game.IsSimulating) //�V���O���v���C
				{
					UnableAxialDrag(machine);
					//isFirstFrame = false;
				}
				else if (StatMaster.isMP && StatMaster.isHosting) //�z�X�g
				{
					/*
					try
					{
						foreach (PlayerData player in Playerlist.Players)
						{
							//ModConsole.Log(player.name);
							//yamabach, yamabach2 �ƕ\��
							//UnableAxialDrag(player.machine); //�����ŃN���C�A���g������������Ȃ� //�����machine��null�ɂȂ��Ă���\�� //�z�X�g�}�V�������H
							//�d���Ƃ��ɂ������Z����Ă��Ȃ������ł́H //�V�~���J�n�����SimulatingBlocks�����݂��Ȃ��I
							UnableAxialDrag(player.machine);
						}
						isFirstFrame = false;
					}
					catch
					{
						isFirstFrame = true;
					}
					
					foreach (PlayerData player in Playerlist.Players) //�z�X�g�̃V�~�����ɃN���C�A���g���V�~�����J�n�����疳���ɂȂ��Ă���
					{
						if (player.machine.isSimulating)
						{
							UnableAxialDrag(player.machine);
						}
					}
				}
				else if (StatMaster.isMP && StatMaster.isClient && Game.IsSimulatingLocal) //�N���C�A���g�̃��[�J���V�~�� //ok
				{
					UnableAxialDrag(machine);
					//isFirstFrame = false;
				}
			}
			
			if (!Game.IsSimulating || !Game.IsSimulatingGlobal || !Game.IsSimulatingLocal)
			{
				//isFirstFrame = true;
			}
			*/
		}
		public void AddScript(Block block)
		{
			BlockBehaviour internalObject = block.InternalObject;
			if (internalObject.BlockID == (int)BlockType.BuildEdge || internalObject.BlockID == (int)BlockType.BuildNode)
            {
				return; // �m�[�h���G�b�W�Ȃ牽�����Ȃ�
            }
			try
			{
				if (internalObject.BlockID == (int)BlockType.FlyingBlock && internalObject.GetComponent<FlyingBlockController>() == null)
				{
					internalObject.gameObject.AddComponent(typeof(FlyingBlockController));
				}
				else if (internalObject.BlockID == (int)BlockType.BuildSurface && internalObject.GetComponent<BuildSurfaceController>() == null)
				{
					internalObject.gameObject.AddComponent(typeof(BuildSurfaceController));
				}
				else if (internalObject.BlockID == (int)BlockType.Spring && internalObject.GetComponent<SpringController>() == null)
				{
					internalObject.gameObject.AddComponent(typeof(SpringController));
				}
				else if (internalObject.BlockID == (int)BlockType.StartingBlock && internalObject.GetComponent<StartingBlockController>() == null)
                {
					internalObject.gameObject.AddComponent(typeof(StartingBlockController));
                }
				else if (internalObject.GetComponent<BlockDragController>() == null)
				{
					internalObject.gameObject.AddComponent(typeof(BlockDragController));
					//ModConsole.Log("Success : AddComponent.");
				}
				if (internalObject.GetComponent<BlockGravity>() == null)
                {
					internalObject.gameObject.AddComponent(typeof(BlockGravity));
                }
				
			}
			catch
			{
				Mod.Error("AddScript Error.");
			}
		}
		public void AddScript(Entity entity)
		{
			LevelEntity internalObject = entity.InternalObject;
			try
			{
				if (internalObject.GetComponent<EntityDragController>() == null)
				{
					internalObject.gameObject.AddComponent(typeof(EntityDragController));
				}
			}
			catch
			{
				Mod.Log("AddScript Error");
			}
		}
	}
	/// <summary>
	/// �u���b�N��{
	/// </summary>
	public abstract class AbstractBlockScript : MonoBehaviour
	{
		[Obsolete]
		public Action<XDataHolder> BlockDataLoadEvent;
		[Obsolete]
		public Action<XDataHolder> BlockDataSaveEvent;
		public Action BlockPropertiseChangedEvent;
		public bool isFirstFrame;
		public BlockBehaviour BB { internal set; get; }
		public bool CombatUtilities { set; get; }


		protected void Awake()
		{
			BB = GetComponent<BlockBehaviour>();
			SafeAwake();
			ChangedProperties();
			try
			{
				BlockPropertiseChangedEvent();
			}
			catch
			{

			}
			DisplayInMapper(CombatUtilities);
		}
		protected void Update()
		{
			if (BB.isSimulating)
			{
				if (isFirstFrame)
				{
					isFirstFrame = false;
					if (CombatUtilities)
					{
						OnSimulateStart();
					}
					if (!StatMaster.isClient)
					{
						ChangeParameter();
					}
				}
				if (CombatUtilities)
				{
					if (StatMaster.isHosting)
					{
						SimulateUpdateHost();
					}
					if (StatMaster.isClient)
					{
						SimulateUpdateClient();
					}
					SimulateUpdateAlways();
				}
			}
			else
			{
				if (CombatUtilities)
				{
					BuildingUpdate();
				}
				isFirstFrame = true;
			}
		}
		protected void FixedUpdate()
		{
			if (CombatUtilities && BB.isSimulating && !isFirstFrame)
			{
				SimulateFixedUpdateAlways();
			}
		}
		protected void LateUpdate()
		{
			if (CombatUtilities && BB.isSimulating && !isFirstFrame)
			{
				SimulateLateUpdateAlways();
			}
		}

		[Obsolete]
		private void SaveConfiguration(PlayerMachineInfo pmi)
		{
			ConsoleController.ShowMessage("On save en");
			if (pmi != null)
			{
				foreach (Modding.Blocks.BlockInfo current in pmi.Blocks)
				{
					if (current.Guid == BB.Guid)
					{
						XDataHolder data = current.Data;
						try
						{
							BlockDataSaveEvent(data);
						}
						catch
						{

						}
						this.SaveConfiguration(data);
						break;
					}
				}
			}
		}
		[Obsolete]
		private void LoadConfiguration()
		{
			ConsoleController.ShowMessage("On load en");
			if (SingleInstance<AddScriptManager>.Instance.PMI != null)
			{
				foreach (Modding.Blocks.BlockInfo current in SingleInstance<AddScriptManager>.Instance.PMI.Blocks)
				{
					if (current.Guid == BB.Guid)
					{
						XDataHolder data = current.Data;
						try
						{
							BlockDataLoadEvent(data);
						}
						catch { }
						LoadConfiguration(data);
						break;
					}
				}
			}
		}
		[Obsolete]
		public virtual void SaveConfiguration(XDataHolder BlockData) { }
		[Obsolete]
		public virtual void LoadConfiguration(XDataHolder BlockData) { }
		public virtual void SafeAwake() { }
		public virtual void OnSimulateStart() { }
		public virtual void SimulateUpdateHost() { }
		public virtual void SimulateUpdateClient() { }
		public virtual void SimulateUpdateAlways() { }
		public virtual void SimulateFixedUpdateAlways() { }
		public virtual void SimulateLateUpdateAlways() { }
		public virtual void BuildingUpdate() { }
		public virtual void DisplayInMapper(bool value) { }
		public virtual void ChangedProperties() { }
		public virtual void ChangeParameter() { }
		public static void SwitchMatalHardness(int Hardness, ConfigurableJoint CJ)
		{
			if (Hardness != 1)
			{
				if (Hardness != 2)
				{
					CJ.projectionMode = JointProjectionMode.None;
				}
				else
				{
					CJ.projectionMode = JointProjectionMode.PositionAndRotation;
					CJ.projectionAngle = 0f;
				}
			}
			else
			{
				CJ.projectionMode = JointProjectionMode.PositionAndRotation;
				CJ.projectionAngle = 0.5f;
			}
		}
		public static void SwitchWoodHardness(int Hardness, ConfigurableJoint CJ)
		{
			switch (Hardness)
			{
				case 0:
					CJ.projectionMode = JointProjectionMode.PositionAndRotation;
					CJ.projectionAngle = 10f;
					CJ.projectionDistance = 5f;
					return;
				case 2:
					CJ.projectionMode = JointProjectionMode.PositionAndRotation;
					CJ.projectionAngle = 5f;
					CJ.projectionDistance = 2.5f;
					return;
				case 3:
					CJ.projectionMode = JointProjectionMode.PositionAndRotation;
					CJ.projectionAngle = 0f;
					CJ.projectionDistance = 0f;
					return;
				default:
					CJ.projectionMode = JointProjectionMode.None;
					CJ.projectionDistance = 0f;
					CJ.projectionAngle = 0f;
					return;
			}
		}
		public AbstractBlockScript()
		{
			CombatUtilities = true;
			isFirstFrame = true;
		}

	}
	public class BlockDragController : AbstractBlockScript
	{
		public bool hasEnabled = true; //���łɋ�C���L���ȏꍇ
		public bool hasUnabled = false; //���łɋ�C�������ȏꍇ
		public float OriginalDrag = 0;
		public Vector3 OriginalAxisDrag = Vector3.zero;
		public override void SafeAwake()
		{
			if (BB.Rigidbody != null)
			{
				if (BB.Rigidbody.drag != 0)
				{
					OriginalDrag = BB.Rigidbody.drag;
				}
			}
			if (BB.GetComponent<AxialDrag>() != null)
			{
				if (BB.GetComponent<AxialDrag>().AxisDrag != Vector3.zero)
				{
					OriginalAxisDrag = BB.GetComponent<AxialDrag>().AxisDrag;
				}
			}
			if (AddScriptManager.Instance.isUnableAxialDrag && hasEnabled) //�g�O��ON���^��łȂ��Ƃ�
			{
				UnableAxialDrag();
			}
			else if (!AddScriptManager.Instance.isUnableAxialDrag && hasUnabled) //�g�O��OFF���^��̂Ƃ�
			{
				EnableAxialDrag();
			}
		}
		public override void BuildingUpdate() //��������x��Ă��܂� //ON�ɂ�������̃V�~���ł͔��f����Ȃ�
		{
			if (AddScriptManager.Instance.isUnableAxialDrag && hasEnabled) //�g�O��ON���^��łȂ��Ƃ�
			{
				UnableAxialDrag();
			}
			else if (!AddScriptManager.Instance.isUnableAxialDrag && hasUnabled) //�g�O��OFF���^��̂Ƃ�
			{
				EnableAxialDrag();
			}
		}
		public virtual void UnableAxialDrag()
		{
			BlockBehaviour simBB = BB.hasSimBlock ? BB.SimBlock : BB.BuildingBlock;
			if (simBB == null)
			{
				simBB = BB;
			}
			AxialDrag component = simBB.GetComponent<AxialDrag>();
			if (component != null && !simBB.noRigidbody)
			{
				component.AxisDrag = Vector3.zero;
			}
			simBB.Rigidbody.drag = 0;
			hasUnabled = true;
			hasEnabled = false;
		}
		public virtual void EnableAxialDrag()
		{
			BlockBehaviour simBB = BB.hasSimBlock ? BB.SimBlock : BB.BuildingBlock;
			if (simBB == null)
			{
				simBB = BB;
			}
			AxialDrag component = simBB.GetComponent<AxialDrag>();
			if (component != null && !simBB.noRigidbody)
			{
				component.AxisDrag = OriginalAxisDrag;
			}
			simBB.Rigidbody.drag = OriginalDrag;
			hasUnabled = false;
			hasEnabled = true;
		}
	}
	public class FlyingBlockController : BlockDragController
	{
		FlyingController FC;
		public override void SafeAwake()
		{
			FC = (FlyingController)BB;
			if (BB.Rigidbody.drag != 0)
			{
				OriginalDrag = BB.Rigidbody.drag;
			}
			if (BB.GetComponent<AxialDrag>() != null)
			{
				if (BB.GetComponent<AxialDrag>().AxisDrag != Vector3.zero)
				{
					OriginalAxisDrag = BB.GetComponent<AxialDrag>().AxisDrag;
				}
			}
			if (AddScriptManager.Instance.isUnableAxialDrag && hasEnabled) //�g�O��ON���^��łȂ��Ƃ�
			{
				UnableAxialDrag();
			}
			else if (!AddScriptManager.Instance.isUnableAxialDrag && hasUnabled) //�g�O��OFF���^��̂Ƃ�
			{
				EnableAxialDrag();
			}
		}
		public override void SimulateFixedUpdateAlways()
		{
			if (AddScriptManager.Instance.isUnableAxialDrag)
			{
				if (FC.flying)
				{
					FC.gameObject.GetComponent<Rigidbody>().drag = 0;
				}
				else
				{
					FC.gameObject.GetComponent<Rigidbody>().drag = 0;
				}
			}
		}
	}
	public class BuildSurfaceController : BlockDragController
	{
		BuildSurface BS;
		public override void SafeAwake()
		{
			BS = (BuildSurface)BB;
			if (BS.wood.dragMultiplier != 0)
			{
				OriginalDrag = BS.wood.dragMultiplier;
			}
			if (AddScriptManager.Instance.isUnableAxialDrag && hasEnabled) //�g�O��ON���^��łȂ��Ƃ�
			{
				UnableAxialDrag();
			}
			else if (!AddScriptManager.Instance.isUnableAxialDrag && hasUnabled) //�g�O��OFF���^��̂Ƃ�
			{
				EnableAxialDrag();
			}
		}
		public override void UnableAxialDrag()
		{
			BS.wood.dragMultiplier = 0;
			hasUnabled = true;
			hasEnabled = false;
		}
		public override void EnableAxialDrag()
		{
			BS.wood.dragMultiplier = OriginalDrag;
			hasUnabled = false;
			hasEnabled = true;
		}
	}
	public class SpringController : BlockDragController
	{
		public Rigidbody[] rigids;
		public override void SafeAwake()
		{
			rigids = new Rigidbody[2];
			for (int i=0; i<2; i++)
			{
				rigids[i] = BB.transform.GetChild(i).gameObject.GetComponent<Rigidbody>();
			}
			if (rigids[0].drag != 0)
			{
				OriginalDrag = rigids[0].drag;
			}
			if (AddScriptManager.Instance.isUnableAxialDrag && hasEnabled) //�g�O��ON���^��łȂ��Ƃ�
			{
				UnableAxialDrag();
			}
			else if (!AddScriptManager.Instance.isUnableAxialDrag && hasUnabled) //�g�O��OFF���^��̂Ƃ�
			{
				EnableAxialDrag();
			}
		}
		public override void UnableAxialDrag()
		{
			foreach (Rigidbody rigid in rigids)
			{
				rigid.drag = 0;
			}
			hasUnabled = true;
			hasEnabled = false;
		}
		public override void EnableAxialDrag()
		{
			foreach (Rigidbody rigid in rigids)
			{
				rigid.drag = OriginalDrag;
			}
			hasUnabled = false;
			hasEnabled = true;
		}
	}
	public class StartingBlockController : BlockDragController
    {
        public override void SafeAwake()
        {
            base.SafeAwake();
			//Mod.Log("StartingBlockController SafeAwake");
        }
        public override void OnSimulateStart()
        {
            base.OnSimulateStart();
			Gravity.UpdateBlocks();
			//Gravity.UpdateEntity();
		}
    }

	// ���͂��������Ƀu���b�N���Q�Ƃ��₷�����邽�߂̃N���X
	public class BlockGravity : AbstractBlockScript
    {
		public Rigidbody rigid;
		/// <summary>
		/// ���ʂ������Ȃ����A���ʂ�0�̏ꍇ��true
		/// </summary>
		public bool zeroMass;
		/// <summary>
		/// ���d�̓u���b�N�������ǂ���
		/// </summary>
		public AntiGravityBlock balance;
		/// <summary>
		/// ���d�̓u���b�N�ł������Ƃ��āA���d�͂��I���ɂȂ��Ă��邩�ǂ���
		/// </summary>
		public bool isBalancingBlock;
		/// <summary>
		/// ���̃u���b�N���X�^�u���ł��邩�ǂ���
		/// </summary>
		public bool IsStartingBlock
        {
            get
            {
				return BB.BlockID == (int)BlockType.StartingBlock;
            }
        }
		/// <summary>
		/// �r���h�]�[���̊p�x
		/// </summary>
		public Quaternion zoneRotation = Quaternion.identity;
		/// <summary>
		/// �d�͌�����󂯂Ă���͂̍��v
		/// ������̍��v�l���J�����̏�����ɂȂ�悤�ɁA�J�����̎p������Ɏg�p
		/// </summary>
		public List<Vector3> ReceivedForceList;
		/// <summary>
		/// ���̃u���b�N�̃}�V���̃r���h�]�[���i�}���`����j
		/// </summary>
		public Transform buildingZone;
		/// <summary>
		/// �V�~�����Ƀr���h�]�[���̊p�x���ύX���ꂽ���ǂ���
		/// </summary>
		//public bool buildingZoneRotated { get; private set; }

		public override void SafeAwake()
        {
			balance = GetComponent<AntiGravityBlock>();
			isBalancingBlock = balance != null;
			SetZeroMass();
			ReceivedForceList = new List<Vector3>();
			if (StatMaster.isMP)
			{
				buildingZone = (BB.ParentMachine as ServerMachine).player.buildZone.transform;
			}
        }
		/// <summary>
		/// zeroMass�̒l���X�V
		/// </summary>
		public void SetZeroMass()
		{
			rigid = GetComponent<Rigidbody>();
			zeroMass = rigid == null;

			// rigidbody���������Ƃ��āA���ʂ�0�Ȃ�true
			if (!zeroMass)
			{
				zeroMass = rigid.mass == 0;
			}
			//Mod.Log($"rigid: {rigid != null}, zeroMass: {zeroMass}");
		}
		/// <summary>
		/// �V�~���I�����Ƀr���h�u���b�N�ŌĂяo��
		/// </summary>
        public void OnEnable()
        {
			// �V�~���I�����Ƀr���h�]�[���̊p�x�����ɖ߂��������K�v
			// ���̂��߂ɁA�V�~���J�n���̃r���h�]�[���̊p�x��ۑ����邱�Ƃ��K�v DONE

			// �ۑ������V�~���J�n���̃r���h�]�[���̊p�x�ɖ߂�
			// �Ă΂�Ȃ��I���V�~�����͂��̃R���|�[�l���g������������Ă���A�ϐ����ω����Ă��Ȃ����߂Ǝv����
			if (StatMaster.isMP && IsStartingBlock)
			{
				buildingZone.rotation = zoneRotation;
				//Mod.Log($"OnEnable rotation = {zoneRotation}");
			}
			
		}
        public override void BuildingUpdate()
        {
            if (StatMaster.isMP && IsStartingBlock)
            {
				// �r���h�]�[���̊p�x��ۑ�
				zoneRotation = buildingZone.rotation;
            }
        }
        public override void OnSimulateStart()
        {

		}
		/// <summary>
		/// �r���h�]�[���̉�]�ɂ���ăJ�����������y������
		/// </summary>
        public override void SimulateLateUpdateAlways()
		{
			//Mod.Log($"{MouseOrbit.Instance.targetType.ToString()}");

			// �����̏d�͌�����͂����������ꍇ�A���̓s�x�L���[�ɗ͂�ۑ����Ă����āA��ł����̕��ς����Ƃ悳����
			if (ReceivedForceList.Count > 0 && IsStartingBlock && StatMaster.isMP)
			{
				// �J�����̏�����ɐݒ肷�����
				Vector3 Sum = Vector3.zero;
				foreach (Vector3 force in ReceivedForceList)
                {
					Sum += force;
                }

				// �p�����킹��]
				// �p����]���x�N�g�����Z�o�i�O�ρj
				Vector3 rotateAxis = Vector3.Cross(buildingZone.up, Sum);

				// ��]������p�x���v�Z
				float rotateAngle = Vector3.Angle(buildingZone.up, Sum);

				// �p�x�ω���1���ȏ�̏ꍇ�ɃJ�����p�x�ύX
				if (rotateAngle > 1f)
				{
					// ��]�N�H�[�^�j�I���𐶐�
					Quaternion rotateQuaternion = Quaternion.AngleAxis(rotateAngle, rotateAxis);

					buildingZone.rotation = rotateQuaternion * buildingZone.rotation;

					// �N���C�A���g���V�~�����Ƀ^�[�Q�b�g��؂�ւ����TargetType��Block�łȂ��Ȃ��Ă��܂��A�r���h�]�[�����Q�Ƃł��Ȃ��Ȃ��Ă��܂�
					// ���J������rotation�𖳗����ς��ɍs���H
					MouseOrbit.Instance.rotation = rotateQuaternion * MouseOrbit.Instance.rotation;
				}

				// ���X�g������
				ReceivedForceList.RemoveAll(x => true);
			}
        }
    }

	// ���͂��������ɃG���e�B�e�B���Q�Ƃ��₷�����邽�߂̃N���X
	public class EntityDragController : MonoBehaviour
	{
		public LevelEntity LE;
		public Rigidbody rigid;
		public bool zeroMass;
		public void Awake()
		{
			LE = GetComponent<LevelEntity>();
			SetZeroMass();
		}
		public void SetZeroMass()
		{
			rigid = GetComponent<Rigidbody>();
			zeroMass = rigid == null;
			if (!zeroMass)
			{
				zeroMass = rigid.mass == 0;
			}
		}
	}
	public class SkyboxController : SingleInstance<SkyboxController>
    {
		public override string Name => "Skybox Controller";
		public GameObject floorObject // ��
        {
			get
			{
				return CurrentEnvironment.Floor;
			}
        }
		public GameObject terrainObject // �n�`
        {
            get
            {
				return CurrentEnvironment.Terrain;
            }
        }

		// �����Ƃ̏��ƒn�`
		public bool Init = false;
		public Environment Barren, Desert, MountainTop, None, Tolbrynd;
		public Environment CurrentEnvironment
        {
            get
            {
				var env = StatMaster.LevelEnvironment;
				switch (env)
                {
					case LevelSettings.LevelEnvironment.Barren:
					default:
						return Barren;
					case LevelSettings.LevelEnvironment.Desert:
						return Desert;
					case LevelSettings.LevelEnvironment.MountainTop:
						return MountainTop;
					case LevelSettings.LevelEnvironment.None:
						return None;
					case LevelSettings.LevelEnvironment.Tolbrynd:
						return Tolbrynd;
                }
            }
        }

		// �X�J�C�{�b�N�X�������鎞�Ɏg��
		public Camera mainCamera;
		public MeshRenderer meshRenderer;
		public Light directionLight;

		// GUI�֌W
		public bool ApplyButton = false;
		public bool ApplyButtonLastFrame = false;
		public bool terrainDeactive = false;
		public bool skyboxChange = false;
		public bool directionLightDeactive = false;
		public bool hide = false; // GUI���������ǂ���

		public void Awake()
		{
			if (!StatMaster.isMP)
            {
				Mod.Log("not MP");
				return;
            }
			GetTerrain();
			GetSkyboxObjects();
			GetDerectionLight();
			Init = false;
		}
		public void Update()
        {
			if (StatMaster.isMP && Init)
			{
				Mod.Log("Initialization");
				GetTerrain();
				GetSkyboxObjects();
				GetDerectionLight();
				Init = false;
			}
			if (ApplyButton && !ApplyButtonLastFrame)
            {
				Apply();
            }
			if (Input.GetKeyDown(KeyCode.Tab))
            {
				hide = !hide;
            }

			// LastFrame�ϐ��̍X�V
			ApplyButtonLastFrame = ApplyButton;
			if (StatMaster.isMainMenu)
            {
				Init = true;
            }
        }

		Rect windowRect = new Rect(0, 0, 200, 150);
		int windowId = ModUtility.GetWindowId();
		void OnGUI()
        {
			if (StatMaster.isMainMenu || !StatMaster.isMP || StatMaster.inMenu || hide)
            {
				return;
            }
			windowRect = GUI.Window(windowId, windowRect, (windwoId) =>
            {
				ApplyButton = GUILayout.Button("Apply");

				// �n�`���������ǂ���
				terrainDeactive = GUILayout.Toggle(terrainDeactive, "Deactive Terrain");

				// �X�J�C�{�b�N�X��ύX���邩�ǂ���
				skyboxChange = GUILayout.Toggle(skyboxChange, "Change Skybox");

				// DirectionLight���������ǂ���
				directionLightDeactive = GUILayout.Toggle(directionLightDeactive, "Deactive Light");

				// ��C��R
				if (!StatMaster.isClient)
				{
					AddScriptManager.Instance.isUnableAxialDrag = GUILayout.Toggle(AddScriptManager.Instance.isUnableAxialDrag, "Unable Drag");
				}

				GUI.DragWindow();
            }, "Universe Mod Controller");
        }

		// �ύX�𔽉f������
		public void Apply()
        {
			GetSkyboxObjects();
			SetTerrain(terrainDeactive);
			SetSkybox(skyboxChange);
			SetDirectionLight(directionLightDeactive);
        }

		// �n�`���擾
		public void GetTerrain()
        {
			Barren = new Environment("MULTIPLAYER LEVEL/FloorBig", "MULTIPLAYER LEVEL/Environments/Barren/BarrenEnv");
			Desert = new Environment("MULTIPLAYER LEVEL/FloorBig");
			MountainTop = new Environment("MULTIPLAYER LEVEL/Environments/MountainTop/FloorBig", "MULTIPLAYER LEVEL/Environments/MountainTop/STATIC");
			None = new Environment("MULTIPLAYER LEVEL/FloorBig");
			Tolbrynd = new Environment("MULTIPLAYER LEVEL/FloorBig");
		}
		// �X�J�C�{�b�N�X�֌W�̃I�u�W�F�N�g���擾
		public void GetSkyboxObjects()
        {
			// Main Camera
			if (mainCamera == null)
			{
				mainCamera = Camera.main;
			}

			// FOG SPHERE
			// Mod.Log("FOG SPHERE sibling index: " + GameObject.Find("Main Camera/FOG SPHERE").transform.GetSiblingIndex().ToString()); ����ȊO�͑S��5�ɂȂ�͗l

			meshRenderer = Camera.main.transform.GetChild(7).GetComponent<MeshRenderer>();
			if (meshRenderer == null)
            {
				Mod.Error("meshRenderer is null! Got one from sibling 5 instead.");
				meshRenderer = GameObject.Find("Main Camera/FOG SPHERE").GetComponent<MeshRenderer>();
			}
		}
		// DirectionLight���擾
		public void GetDerectionLight()
        {
			if (GameObject.Find("MULTIPLAYER LEVEL/Environments/Directional light") == null)
            {
				Mod.Error("Directional Light is null!");
            }
			if (directionLight == null)
            {
				directionLight = GameObject.Find("MULTIPLAYER LEVEL/Environments/Directional light").GetComponent<Light>();
				if (directionLight == null)
                {
					Mod.Error("Light component is null!"); //�łȂ�
                }
            }
        }

		// �n�`���o������������肷��
		public void SetTerrain(bool isOn)
        {
			if (floorObject != null) // ��������
			{
				floorObject.SetActive(!isOn);
			}
			if (terrainObject != null) // �n�`������
			{
				terrainObject.SetActive(!isOn);
			}
		}

		// �X�J�C�{�b�N�X���F���ɂ�����߂����肷��
		public void SetSkybox(bool isOn)
        {
			meshRenderer.material.color = isOn ? Color.black : Color.white;
			var colorfulFog = mainCamera.GetComponent<ColorfulFog>();
			colorfulFog.enabled = !isOn;
        }

		// DirectionLight����������߂����肷��
		public void SetDirectionLight(bool isOn)
        {
			directionLight.enabled = !isOn;
        }

		// �����Ƃ̏��ƒn�`
		public class Environment
		{
			public GameObject Floor, Terrain;
			public Environment(string f, string t = "")
			{
				Find(f, t);
			}
			public void Find(string f, string t = "")
			{
				// ��
				if (Floor == null)
				{
					Floor = GameObject.Find(f);
				}
				if (Floor == null)
                {
					Mod.Error("can't find gameObject named " + f);
                }

				// �n�`
				if (t == "")
                {
					Terrain = null;
					return;
                }
				if (Terrain == null)
				{
					Terrain = GameObject.Find(t);
				}
				if (Terrain == null)
                {
					Mod.Error("can't find gameObject named " + t);
                }
			}
        }

		/*
		public void getObjects()
        {
			var levelNames = new List<Level>
			{
				new Level("MULTIPLAYER LEVEL/FloorBig", "MULTIPLAYER LEVEL/Environments/Barren/BarrenEnv"),

			};
			for (int i=0; i<levelNames.Count; i++)
            {
				if (GameObject.Find(levelNames[i].sphereName) != null)
                {
					getObjectsFrom(levelNames[i]);
                }
            }
        }
		public void getObjectsFrom(Level level) // skyObject��florObject���擾
		{
			// ��
			if (floorObject == null)
			{
				floorObject = GameObject.Find(level.floorName);
			}
			if (floorDeactive && floorObject.activeSelf)
			{
				floorObject.SetActive(false);
			}
			else if (!floorObject.activeSelf)
            {
				floorObject.SetActive(true);
            }

			// �n�`
			if (terrainObject == null)
            {
				terrainObject = GameObject.Find(level.terrainName);
            }
			if (floorDeactive && terrainObject.activeSelf)
            {
				terrainObject.SetActive(false);
            }
			else if (!terrainObject.activeSelf)
            {
				terrainObject.SetActive(true);
            }
        }
		public struct Level
        {
			public string floorName;
			public string terrainName;
			public Level(string sphere, string floor, string terrain="")
            {
				floorName = floor;
				terrainName = terrain;
            }
        }
		*/
    }
}
