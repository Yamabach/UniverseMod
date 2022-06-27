using UnityEngine;
using Modding;
using Modding.Blocks;
using Modding.Serialization;

namespace UDspace
{
    // 斥力物質
    public class AntiGravityBlock : BlockScript
    {
        public Rigidbody rigid;
        public MKey FloatKey;
        public MSlider mBuoyancy, mMass, mDrag, mAngularDrag;
        public MToggle Toggle;
        public bool isOn = false;
        public bool wasOn = false; // 1フレーム前のisOnの値

        public MeshFilter crystalFilter;
        public MeshRenderer crystalRenderer;
        public Material crystalMat;
        public GameObject crystal; // クリスタルのゲームオブジェクト
        public ModMesh MeshCrystal; // クリスタル
        public ModTexture TexOff, TexOn, TexReverse; // クリスタルオフ、クリスタルオン、クリスタル負値

        // エミュレート対応
        public override bool EmulatesAnyKeys => true;

        public override void SafeAwake()
        {
            InitializeModResources();
            //Mod.Log("initailizeModResources done");
            if (!IsSimulating)
            {
                GenerateCrystal();
                //Mod.Log("generateCrystal done");
            }

            rigid = GetComponent<Rigidbody>();

            FloatKey = AddKey("Float", "float", KeyCode.B);

            mBuoyancy = AddSlider("Buoyancy", "buoyancy", 1f, 0f, 2f);
            mMass = AddSlider("Mass", "mass", 0.5f, 0.2f, 10f);
            mDrag = AddSlider("Drag", "drag", 0.1f, 0f, 10f);
            mAngularDrag = AddSlider("Angular Drag", "angular-drag", 0f, 0f, 10f);

            Toggle = AddToggle("Toggle", "toggle", true);

            mMass.ValueChanged += delegate
            {
                rigid.mass = mMass.Value;
            };
            mDrag.ValueChanged += delegate
            {
                rigid.drag = mDrag.Value;
            };
            mAngularDrag.ValueChanged += delegate
            {
                rigid.angularDrag = mAngularDrag.Value;
            };
        }

        public override void OnSimulateStart()
        {
            // クリスタルをOffにする
            DeactivateCrystal();
        }

        public override void OnSimulateStop()
        {
            // クリスタルをOffにする
            DeactivateCrystal();
        }

        public override void SimulateUpdateAlways()
        {
            // 斥力有効化の処理
            if (Toggle.IsActive) // トグル
            {
                if (FloatKey.IsPressed)
                {
                    isOn = !isOn;
                }
            }
            else // 押した場合のみ反応
            {
                isOn = FloatKey.IsHeld;
            }

            // テクスチャ回りの変更
            if (isOn && !wasOn)
            {
                if (mBuoyancy.Value > 0)
                {
                    ActivateCrystal();
                }
                else if (mBuoyancy.Value < 0)
                {
                    ReverseCrystal();
                }
                else // mBuouancy.Value == 0
                {
                    DeactivateCrystal();
                }
            }
            if (!isOn && wasOn)
            {
                DeactivateCrystal();
            }
            wasOn = isOn;

            // 重力下の場合は上向きに力をかける
            if (isOn && !StatMaster.GodTools.GravityDisabled)
            {
                rigid.AddForce(UnityEngine.Vector3.up * mBuoyancy.Value * 32.81f);
            }
        }
        public override void KeyEmulationUpdate()
        {
            // 斥力有効化の処理
            if (Toggle.IsActive) // トグル
            {
                if (FloatKey.EmulationPressed())
                {
                    isOn = !isOn;
                }
            }
            else // 押した場合のみ反応
            {
                isOn = FloatKey.EmulationHeld();
            }
        }

        // メッシュとテクスチャを取得する
        public void InitializeModResources()
        {
            //public ModMesh MeshFull, MeshBase, MeshCrystal; // 全部、基部、クリスタル
            //public ModTexture TexFull, TexBase, TexOff, TexOn; // 全部、基部、クリスタルオフ、クリスタルオン
            MeshCrystal = ModMesh.GetMesh("crystal");
            TexOff = ModTexture.GetTexture("crystal_off");
            TexOn = ModTexture.GetTexture("crystal_on");
            TexReverse = ModTexture.GetTexture("crystal_reverse");
        }

        // クリスタルを生成し、メッシュとテクスチャを割り振る
        public void GenerateCrystal()
        {
            crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crystal.transform.parent = this.transform;
            crystal.transform.localPosition = UnityEngine.Vector3.forward * 0.25f;
            crystal.transform.localRotation = Quaternion.identity;
            crystal.transform.localScale = UnityEngine.Vector3.one / 2f;
            Destroy(crystal.GetComponent<BoxCollider>());
            crystalFilter = crystal.GetComponent<MeshFilter>();
            crystalRenderer = crystal.GetComponent<MeshRenderer>();
            crystalMat = crystal.GetComponent<Renderer>().material;

            // メッシュを割り振る
            crystalFilter.mesh = MeshCrystal;

            DeactivateCrystal();
        }

        // クリスタルをOffにする
        public void DeactivateCrystal()
        {
            crystalMat.SetTexture("_MainTex", TexOff.Texture);
            //Mod.Log("Deactivate Crystal");
            //Mod.Log(string.Format("crystal is active : {0}", crystal.activeSelf));
        }

        // クリスタルをOnにする
        public void ActivateCrystal()
        {
            crystalMat.SetTexture("_MainTex", TexOn.Texture);
            //Mod.Log("Activate Crystal");
        }

        // クリスタルを負値の色にする
        public void ReverseCrystal()
        {
            crystalMat.SetTexture("_MainTex", TexReverse.Texture);
            //Mod.Log("Reverse Crystal");
        }
    }
}