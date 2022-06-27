using System;
using System.Collections.Generic;
using System.Linq;
using Modding;
using Modding.Blocks;
using Modding.Levels;
using UnityEngine;
using UnityEngine.Rendering;

namespace UDspace
{
	public abstract class Gravity : MonoBehaviour
	{
		public LevelEntity LE;
		public GenericEntity GE;
		public Rigidbody rigid;
		public static List<PlayerData> Players; // 全プレイヤーのデータ
		public static List<BlockBehaviour> Blocks; // 全ブロック（シミュ中）
		public static List<BlockGravity> GravityBlocks; // 全ブロックのBlockGravity
		//public static List<EntityDragController> Entities; // レベル中で物理のある全てのエンティティ
		public static List<EntityDragController> SphericalGravities; // 自身を含む球体引力源エンティティ
		public MToggle attractBlock; // ブロックを吸引する
		//public MToggle attractEntity; // エンティティを吸引する
		public ForceMode mode = ForceMode.Impulse; // 加力の状態
		public Rect debugWindowRect = new Rect(0f, 150f, 200f, 200f); // デバッグ用ウィンドウ
		public int debugWindowId;
		private int sendMessageCycle = 10; // クライアントに見た目を描画する頻度（FixedUpdateの呼び出しごとに更新）
		private int sendMessageTime;
		public int SendMessageTime // プロパティ
        {
            set
            {
				if (value < 0) { value = sendMessageCycle; }
				if (sendMessageCycle < value) { value = 0; }
				sendMessageTime = value;
            }
            get
            {
				return sendMessageTime;
            }
        }
		public bool InSimulation
        {
            get
            {
				return LE.isSimulating || (LE.isStatic && StatMaster.levelSimulating) || Game.IsSimulatingGlobal || (StatMaster.isClient && StatMaster.isLocalSim); // シミュ中かどうか
			}
        }

		public virtual void Awake()
		{
			LE = GetComponent<LevelEntity>();
			GE = GetComponent<GenericEntity>();
			rigid = GetComponent<Rigidbody>();
			GravityBlocks = new List<BlockGravity>();

			// UI生成
			GenerateGUI();

			// デバッグ用GUI初期化
			debugWindowId = ModUtility.GetWindowId();
		}
		public virtual void Start()
		{
			UpdateBlocks();
			//UpdateEntity();
		}
		public virtual void FixedUpdate()
        {
			SendMessageTime++;
			if (InSimulation && !StatMaster.isClient && SendMessageTime == 0)
            {
				ModNetworking.SendToAll(
					Mod.TranslateType.CreateMessage(Entity.From(this.gameObject), transform.position, transform.rotation.eulerAngles)
					);
            }
        }
		public void OnGUI()
        {
			if (!StatMaster.isMainMenu && Mod.debug)
			{
				debugWindowRect = GUI.Window(debugWindowId, debugWindowRect, DebugDisplay, "Debug Window");
			}
		}
		public virtual void DebugDisplay(int windowId) { }

		// ブロックとエンティティの一覧を更新する
		public static void UpdateBlocks()
		{
			Players = Playerlist.Players; // 全プレイヤーを取得
			Blocks = new List<BlockBehaviour>();
			GravityBlocks = new List<BlockGravity>();
			foreach (PlayerData player in Players)
			{
				if (player.machine.isSimulating)
				{
					Blocks.AddRange(player.machine.SimulationBlocks);
				}
			}
			foreach (BlockBehaviour bb in Blocks)
			{
				if (bb.BlockID == (int)BlockType.BuildNode || bb.BlockID == (int)BlockType.BuildEdge) { continue; }
				if (bb.parentBlock != null) { continue; }
				BlockGravity bg = bb.GetComponent<BlockGravity>();
				if (bg != null)
				{
					GravityBlocks.Add(bg);
				}
			}
			//Mod.Log("GravityBlocks update. Count=" + GravityBlocks.Count);
		}
		/*
		public static void UpdateEntity()
		{
			Entities = new List<EntityDragController>();
			SphericalGravities = new List<EntityDragController>();
			foreach (LevelEntity entity in LevelEditor.Instance.Entities)
			{
				EntityDragController dragController = entity.GetComponent<EntityDragController>();
				if (entity.isStatic)
				{
					Mod.Log(entity.name + " is static");
					continue;
				}
				if (dragController != null)
				{
					Entities.Add(dragController);
				}
				SphericalGravity sg = entity.GetComponent<SphericalGravity>();
				if (sg != null)
				{
					SphericalGravities.Add(dragController);
				}
			}
			//Mod.Log("Entities update. Count=" + Entities.Count);
		}
		*/
		public abstract void GenerateGUI();
		public abstract void AddForce(BlockGravity blockGravity);
		public virtual void AddForce(List<BlockGravity> blockGravities)
		{
			// 引力を発生させる
			foreach (BlockGravity block in blockGravities)
			{
				if (block == null)
                {
					continue;
                }
				AddForce(block);
			}
		}
		public abstract void AddForce(EntityDragController entity);
		public virtual void AddForce(List<EntityDragController> entities)
		{
			foreach (EntityDragController entity in entities)
			{
				if (entity.LE.simEntity == LE || entity.LE == LE.simEntity)
				{
					continue;
				}
				AddForce(entity);
			}
		}
	}
    public class SphericalGravity : Gravity
    {
		public float gravitationBlock = 6.7e-1f; // ブロックの引力定数
		//public float gravitationEntity = 6.7f; // ブロックの引力定数の10倍程度 // まだ足りなさそう
		public MSlider density; // このオブジェクトの質量
		public MSlider height; // 引力が適用される半径
		public float threshold12 = 100; // テクスチャの解像度の切り替わり
		public float threshold24 = 400; // テクスチャの解像度の切り替わり
		public TexType texType; // 星の種類
		public MMenu type;
		public List<string> texTypeList = new List<string>()
		{
			"Moon", "Venus",
		};
		public float Scale // スケールの最大値
        {
            get
            {
				return Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
            }
        }
		public ModTexture moonTex1, moonTex2, moonTex4, venusTex1, venusTex2, venusTex4;
		public ModTexture Tex1
        {
            get
            {
                switch (texType)
                {
					default:
					case TexType.Moon:
						return moonTex1;
					case TexType.Venus:
						return venusTex1;
                }
            }
        }
		public ModTexture Tex2
		{
			get
			{
				switch (texType)
				{
					default:
					case TexType.Moon:
						return moonTex2;
					case TexType.Venus:
						return venusTex2;
				}
			}
		}
		public ModTexture Tex4
		{
			get
			{
				switch (texType)
				{
					default:
					case TexType.Moon:
						return moonTex4;
					case TexType.Venus:
						return venusTex4;
				}
			}
		}
		public ModTexture CurTex // 現在使われるべきテクスチャ
        {
            get
            {
				if (Scale < threshold12)
                {
					return Tex1;
                }
				else if (Scale < threshold24)
                {
					return Tex2;
                }
                else
                {
					return Tex4;
                }
            }
        }
		public Material mat;
		public EntityVisualController EVC; // テクスチャ制御？

		//public GameObject GravityRange; // 視覚的なわかりやすさのための球体
		public override void Awake()
		{
			base.Awake();

			// 有効範囲表示球の初期設定
			//GravityRangeInit();

			// 値が変更された際の挙動を設定
            density.ValueChanged += delegate(float value)
			{
				float mass = 4 / 3 * 3.14f * Scale * Scale * Scale * value;
				if (rigid != null) // static状態ならrigid==nullとなる
                {
					rigid.mass = mass;
                }
				//Mod.Log("mass value changed to " + value);
			};
			type.ValueChanged += delegate (int value)
			{
				texType = (TexType)type.Value;
				mat.SetTexture("_MainTex", CurTex);
			};

			InitializeModResources();
			EVC = GetComponent<EntityVisualController>();
			mat = EVC.renderers[0].material;
		}
		public override void Start()
        {
			base.Start();
		}
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (InSimulation)
			{
				if (GravityBlocks != null && attractBlock.IsActive)
				{
					AddForce(GravityBlocks);
				}
				/*
				if (Entities != null && attractEntity.IsActive)
				{
					AddForce(Entities);
				}
				*/
			}
            else
            {
				if (transform.hasChanged)
                {
					if (mat.GetTexture("_MainTex") != CurTex)
					{
						mat.SetTexture("_MainTex", CurTex);
					}
                }
            }
		}
		/*
		public void GravityRangeInit() // 有効範囲表示球の初期設定
        {
			GravityRange = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			GravityRange.transform.parent = transform;
			GravityRange.transform.localPosition = Vector3.zero;
			Destroy(GravityRange.GetComponent<SphereCollider>());
			Renderer rend = GravityRange.GetComponent<Renderer>();
			rend.receiveShadows = false;
			
			foreach (Material material in rend.materials)
            {
				//下記コードでRendering Modeが変更できるが、なくても半透明になる。
                material.SetFloat("_Mode", 2);
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
                material.SetColor("_Color", new Color(1, 1, 1, 0.5f));
            }
			GravityRange.transform.localScale = Vector3.one * radius.Value * 2;
		}
		*/

		public override void GenerateGUI()
		{
			density = GE.AddSlider(Mod.isJapanese ? "密度" : "Density", "density", 500f, 1f, 10000f);
			height = GE.AddSlider(Mod.isJapanese ? "引力適用高度" : "Gravity Application Altitude", "height", 500f, 1f, 10000f);
			attractBlock = GE.AddToggle(Mod.isJapanese ? "ブロックを吸引" : "Attract Blocks", "attract-block", true);
			//attractEntity = GE.AddToggle(Mod.isJapanese ? "エンティティを吸引" : "Attract Entities", "attract-entity", false);
			type = GE.AddMenu("texture-type", 0, texTypeList, true);
		}
		// メッシュとテクスチャを取得する
		public void InitializeModResources()
		{
			//public ModMesh MeshFull, MeshBase, MeshCrystal; // 全部、基部、クリスタル
			//public ModTexture TexFull, TexBase, TexOff, TexOn; // 全部、基部、クリスタルオフ、クリスタルオン
			moonTex1 = ModMesh.GetTexture("moon1");
			moonTex2 = ModMesh.GetTexture("moon2");
			moonTex4 = ModMesh.GetTexture("moon4");
			venusTex1 = ModMesh.GetTexture("venus1");
			venusTex2 = ModMesh.GetTexture("venus2");
			venusTex4 = ModMesh.GetTexture("venus4");
		}
		public override void AddForce(BlockGravity blockGravity)
		{
			// 一定条件で何もしない
			if (blockGravity.rigid == null)
			{
				return;
			}
			if (blockGravity.zeroMass)// 質量0なら何もしない
			{
				return;
			}
			float radius = Scale + height.Value;
			if ((transform.position - blockGravity.transform.position).sqrMagnitude > radius * radius) // 一定の距離以上なら計算しない
            {
				return;
            }
			float distance = Vector3.Distance(transform.position, blockGravity.transform.position);
			Vector3 force = gravitationBlock * density.Value * blockGravity.rigid.mass / (distance * distance * distance) * (transform.position - blockGravity.transform.position);
			if (blockGravity.isBalancingBlock)
            {
				if (blockGravity.balance.isOn)
                {
					force = -blockGravity.balance.mBuoyancy.Value * force;
                }
            }
			blockGravity.rigid.AddForce(force, mode);
			rigid.AddForce(-force, mode);
        }
		public override void AddForce(EntityDragController entity)
		{
			// 一定条件で何もしない
			Rigidbody simRigid = entity.LE.simEntity.GetComponent<Rigidbody>();
			float radius = Scale + height.Value;
			if ((transform.position - simRigid.worldCenterOfMass).sqrMagnitude > radius * radius)// 一定の距離以上なら計算しない
            {
				return;
            }
			if (entity.zeroMass)
            {
				return;
            }
			//Mod.Log(simRigid.worldCenterOfMass.ToString());
			float distance = Vector3.Distance(transform.position, simRigid.worldCenterOfMass);
			Vector3 force = gravitationBlock * density.Value * entity.rigid.mass / (distance * distance * distance) * (transform.position - simRigid.worldCenterOfMass);
			entity.rigid.AddForce(force, mode);
			if (!SphericalGravities.Contains(entity)) // 二重に引力がかかるのを防ぐ
			{
				rigid.AddForce(-force, mode);
			}
			//Mod.Log("add force from " + name + " to " + entity.LE.name + force.magnitude);
		}

		public enum TexType
        {
			Moon, Venus,
        }
	}
	public class PlanarGravity : Gravity
    {
		public readonly float gravitationalAcceleration = 32.81f;
		public MSlider mass; // 質量 引力とは関係無し
		public MSlider distance; // 検知距離
		public MSlider GBlock; // ブロックのG
		//public MSlider GEntity; // エンティティのG
		public Plane plane; // 平面構造体
		public Plane planeForward, planeBackward, planeRight, planeLeft; // 側面判定用
		public Collider collider; // このオブジェクトのコライダー

        public override void Awake()
        {
			base.Awake();
			collider = GetComponentInChildren<Collider>();
			mass.ValueChanged += delegate (float value)
			{
				if (rigid != null) // static状態ならrigid==nullとなる
				{
					rigid.mass = value;
				}
			};

			// planeの初期化
			plane = new Plane(transform.up, transform.position);
			planeForward = new Plane(transform.forward, transform.position + transform.forward * LE.Scale.z * 4f);
			planeBackward = new Plane(-transform.forward, transform.position - transform.forward * LE.Scale.z * 4f);
			planeRight = new Plane(transform.right, transform.position + transform.right * LE.Scale.x * 4f);
			planeLeft = new Plane(-transform.right, transform.position - transform.right * LE.Scale.x * 4f);
		}
        public override void Start()
        {
            base.Start();
        }

        public override void FixedUpdate()
        {
			base.FixedUpdate();
			if (InSimulation)
			{
				plane.SetNormalAndPosition(transform.up, transform.position);
				planeForward.SetNormalAndPosition(transform.forward, transform.position + transform.forward * LE.Scale.z * 4f);
				planeBackward.SetNormalAndPosition(-transform.forward, transform.position - transform.forward * LE.Scale.z * 4f);
				planeRight.SetNormalAndPosition(transform.right, transform.position + transform.right * LE.Scale.x * 4f);
				planeLeft.SetNormalAndPosition(-transform.right, transform.position - transform.right * LE.Scale.x * 4f);

				if (GravityBlocks != null && GBlock.Value != 0f)
				{
					AddForce(GravityBlocks);
				}
				/*
				if (Entities != null && GEntity.Value != 0f)
				{
					AddForce(Entities);
				}
				*/
			}
		}

        public override void GenerateGUI()
        {
			mass = GE.AddSlider(Mod.isJapanese ? "質量" : "Mass", "mass", 50f, 1f, 10000f);
			distance = GE.AddSlider(Mod.isJapanese ? "検知距離" : "Detection Distance", "distance", 10f, 0f, 10000f);
			GBlock = GE.AddSlider(Mod.isJapanese ? "ブロックG" : "Block G", "g-block", 1f, -100f, 100f);
			//GEntity = GE.AddSlider(Mod.isJapanese ? "エンティティG" : "Entity G", "g-entity", 1f, -100f, 100f);
		}

        public override void AddForce(BlockGravity blockGravity)
        {
			if (blockGravity.rigid == null)
            {
				return;
            }
			if (blockGravity.zeroMass)// 質量0なら何もしない
			{
				return;
			}
			var u = blockGravity.transform.position;
			float dis = plane.GetDistanceToPoint(u); // 距離を計測
			if (dis < 0 || distance.Value < dis)
            {
				return;
            }
			Vector3 force = -transform.up * blockGravity.rigid.mass * GBlock.Value;
			if (blockGravity.isBalancingBlock)
			{
				if (blockGravity.balance.isOn)
				{
					force = -blockGravity.balance.mBuoyancy.Value * force;
				}
			}
			blockGravity.rigid.AddForce(force, mode);
			//rigid.AddForce(-force, mode);
			return;
        }
        public override void AddForce(List<BlockGravity> blockGravities)
        {
			foreach (BlockGravity block in blockGravities)
            {
				if (planeForward.GetSide(block.transform.position))
                {
					return;
                }
				if (planeBackward.GetSide(block.transform.position))
                {
					return;
                }
				if (planeRight.GetSide(block.transform.position))
                {
					return;
                }
				if (planeLeft.GetSide(block.transform.position))
                {
					return;
                }
				AddForce(block);
            }
		}
        public override void DebugDisplay(int windowId)
        {
			if (GravityBlocks != null)
			{
				GUILayout.Label(string.Format("GravityBlocks.Count = {0}", GravityBlocks.Count));
			}
            if (GravityBlocks.Count != 0)
            {
				BlockGravity block = GravityBlocks[0];
				var u = block.transform.position;
				float dis = plane.GetDistanceToPoint(u); // 距離を計測
				GUILayout.Label(string.Format("distance = {0}", dis.ToString()));
				GUILayout.Label(string.Format("plane forward : {0}", planeForward.GetSide(block.transform.position)));
				GUILayout.Label(string.Format("plane backward : {0}", planeBackward.GetSide(block.transform.position)));
				GUILayout.Label(string.Format("plane right : {0}", planeRight.GetSide(block.transform.position)));
				GUILayout.Label(string.Format("plane left : {0}", planeLeft.GetSide(block.transform.position)));
            }
        }
        public override void AddForce(EntityDragController entity)
        {
			if (entity.rigid == null)
            {
				return;
            }
			if (entity.zeroMass)// 質量0なら何もしない
			{
				return;
			}
			Vector3 force = -transform.up * entity.rigid.mass * GBlock.Value;
			entity.rigid.AddForce(force, mode);
			//rigid.AddForce(-force, mode);
			return;
		}
        public override void AddForce(List<EntityDragController> entities)
        {
			foreach (EntityDragController entity in entities)
			{
				var u = entity.transform.position;
				float dis = plane.GetDistanceToPoint(u); // 距離を計測
				//Vector3 foot = u - (Vector3.Dot(plane.normal, u - transform.position)) * plane.normal; // 平面への投影を計測
				//bool contain = Physics.OverlapSphere(foot, 0).Any(col => col == collider);
				if (0f < dis && dis < distance.Value)
				{
					AddForce(entity);
				}
			}
		}
    }
	public class ColonyCore : Gravity
    {
		public readonly float gravitationalAcceleration = -32.81f;
		public MSlider mass; // 質量 引力とは関係無し
		public MSlider distanceMin, distanceMax; // 検知距離
		public MSlider GBlock; // ブロックのG
		//public MSlider GEntity; // エンティティのG
		public Plane planeTop, planeBottom; // 平面構造体
		public CapsuleCollider collider; // このオブジェクトのコライダー
		public override void Awake()
		{
			base.Awake();
			collider = GetComponentInChildren<CapsuleCollider>();
			mass.ValueChanged += delegate (float value)
			{
				if (rigid != null) // static状態ならrigid==nullとなる
				{
					rigid.mass = value;
				}
			};

			// planeの初期化
			planeTop = new Plane(transform.up, transform.position + transform.up * LE.Scale.y * 2f);
			planeBottom = new Plane(-transform.up, transform.position - transform.up * LE.Scale.y * 2f); // 後で係数を調整する
		}
		public override void Start()
		{
			base.Start();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (InSimulation)
			{
				planeTop.SetNormalAndPosition(transform.up, transform.position + transform.up * LE.Scale.y * 2f);
				planeBottom.SetNormalAndPosition(-transform.up, transform.position - transform.up * LE.Scale.y * 2f);
				if (GravityBlocks != null && GBlock.Value != 0f)
				{
					AddForce(GravityBlocks);
				}
				/*
				if (Entities != null && GEntity.Value != 0f)
				{
					AddForce(Entities);
				}
				*/
			}
		}

		public override void GenerateGUI()
		{
			mass = GE.AddSlider(Mod.isJapanese ? "質量" : "Mass", "mass", 50f, 1f, 10000f);
			distanceMin = GE.AddSlider(Mod.isJapanese ? "最低検知距離" : "Min Detection Distance", "distance-min", 10f, 0f, 10000f);
			distanceMax = GE.AddSlider(Mod.isJapanese ? "最大検知距離" : "Max Detection Distance", "distance-max", 30f, 0f, 10000f);
			GBlock = GE.AddSlider(Mod.isJapanese ? "ブロックG" : "Block G", "g-block", 1f, -100f, 100f);
			//GEntity = GE.AddSlider(Mod.isJapanese ? "エンティティG" : "Entity G", "g-entity", 1f, -100f, 100f);
		}

		public override void AddForce(BlockGravity blockGravity)
		{
			if (blockGravity.rigid == null)
			{
				return;
			}
			if (blockGravity.zeroMass)// 質量0なら何もしない
			{
				return;
			}
			// 距離と軸への射影を取得する
			float length = Vector3.Dot(transform.up, blockGravity.transform.position - transform.position);
			float dis = Vector3.Distance(blockGravity.transform.position, length * transform.up + transform.position);
			if (dis < distanceMin.Value || distanceMax.Value < dis)
            {
				return;
            }

			Vector3 force = -(length * transform.up + transform.position - blockGravity.transform.position).normalized * blockGravity.rigid.mass * GBlock.Value;
			if (blockGravity.isBalancingBlock)
			{
				if (blockGravity.balance.isOn)
				{
					force = -blockGravity.balance.mBuoyancy.Value * force;
				}
			}
			blockGravity.rigid.AddForce(force, mode);
			//rigid.AddForce(-force, mode);
			return;
		}
		public override void AddForce(List<BlockGravity> blockGravities)
		{
			foreach (BlockGravity block in blockGravities)
			{
				// 軸の側面上にいなければ何もしない
				if (planeTop.GetSide(block.transform.position))
                {
					return;
                }
				if (planeBottom.GetSide(block.transform.position))
                {
					return;
                }
				AddForce(block);
			}
		}
		public override void DebugDisplay(int windowId)
		{
			if (GravityBlocks != null)
			{
				GUILayout.Label(string.Format("GravityBlocks.Count = {0}", GravityBlocks.Count));
			}
			if (GravityBlocks.Count != 0)
			{
				BlockGravity block = GravityBlocks[0];
				var u = block.transform.position;
			}
		}
		public override void AddForce(EntityDragController entity)
		{
			if (entity.rigid == null)
			{
				return;
			}
			if (entity.zeroMass)// 質量0なら何もしない
			{
				return;
			}
			// 距離と軸への射影を取得する
			float length = Vector3.Dot(transform.up, entity.transform.position - transform.position);
			float dis = Vector3.Distance(entity.transform.position, length * transform.up + transform.position);
			if (dis < distanceMin.Value || distanceMax.Value < dis)
			{
				return;
			}

			Vector3 force = -(length * transform.up + transform.position - entity.transform.position).normalized * entity.rigid.mass * GBlock.Value;
			entity.rigid.AddForce(force, mode);
			//rigid.AddForce(-force, mode);
			return;
		}
		public override void AddForce(List<EntityDragController> entities)
		{
			foreach (EntityDragController entity in entities)
			{
				// 軸の側面上にいなければ何もしない
				if (planeTop.GetSide(entity.transform.position))
				{
					return;
				}
				if (planeBottom.GetSide(entity.transform.position))
				{
					return;
				}
				AddForce(entity);
			}
		}
	}
}