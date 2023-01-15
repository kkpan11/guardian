using System;
using System.Collections;
using UnityEngine;

public class Bullet : Photon.MonoBehaviour, Anarchy.Custom.Interfaces.IAnarchyScriptHook
{
    private GameObject master;
    private Vector3 velocity = Vector3.zero;
    private Vector3 velocity2 = Vector3.zero;
    public GameObject rope;
    public LineRenderer lineRenderer;
    private ArrayList nodes = new ArrayList();
    private ArrayList spiralNodes;
    private int phase;
    private float killTime;
    private float killTime2;
    private bool left = true;
    public bool leviMode;
    public float leviShootTime;
    private GameObject myRef;
    private int spiralcount;
    private bool isdestroying;
    public TITAN myTitan;

    // BEGIN Guardian
    public float tileScale = 1f;
    public bool petraMode;
    // END Guardian

    // BEGIN Anarchy
    public Transform Transform
    {
        get
        {
            return transform;
        }
    }

    public Anarchy.Custom.Interfaces.IAnarchyScriptHero Master
    {
        get
        {
            return master?.GetComponent<HERO>();
        }
    }

    void Anarchy.Custom.Interfaces.IAnarchyScriptHook.KillMaster()
    {
        master.GetComponent<HERO>().photonView.RPC("netDie2", PhotonTargets.All, -1, "Trap");
    }
    // END: Anarchy

    public void Launch(Vector3 v, Vector3 v2, string launcherRef, bool isLeft, GameObject master, bool leviMode = false, bool petraMode = false)
    {
        if (phase != 2)
        {
            this.master = master;
            velocity = v;
            float f = Mathf.Acos(Vector3.Dot(v.normalized, v2.normalized)) * 57.29578f;
            if (Mathf.Abs(f) > 90f)
            {
                velocity2 = Vector3.zero;
            }
            else
            {
                velocity2 = Vector3.Project(v2, v);
            }

            myRef = launcherRef switch
            {
                "hookRefL1" => master.GetComponent<HERO>().hookRefL1,
                "hookRefL2" => master.GetComponent<HERO>().hookRefL2,
                "hookRefR1" => master.GetComponent<HERO>().hookRefR1,
                "hookRefR2" => master.GetComponent<HERO>().hookRefR2,
                _ => null
            };

            nodes = new ArrayList
            {
                myRef.transform.position
            };
            phase = 0;
            this.leviMode = leviMode;
            this.petraMode = petraMode;
            left = isLeft;
            if (IN_GAME_MAIN_CAMERA.Gametype != 0 && base.photonView.isMine)
            {
                base.photonView.RPC("myMasterIs", PhotonTargets.Others, master.GetComponent<HERO>().photonView.viewID, launcherRef);
                base.photonView.RPC("setVelocityAndLeft", PhotonTargets.Others, v, velocity2, left);
            }
            base.transform.position = myRef.transform.position;
            base.transform.rotation = Quaternion.LookRotation(v.normalized);
        }
    }

    [Guardian.Networking.RPC(Name = "myMasterIs")]
    private void MyMasterIs(int viewId, string launcherRef, PhotonMessageInfo info)
    {
        if (Guardian.AntiAbuse.Validators.HookValidator.IsHookMasterSetValid(this, viewId, info))
        {
            master = PhotonView.Find(viewId).gameObject;

            myRef = launcherRef switch
            {
                "hookRefL1" => master.GetComponent<HERO>().hookRefL1,
                "hookRefL2" => master.GetComponent<HERO>().hookRefL2,
                "hookRefR1" => master.GetComponent<HERO>().hookRefR1,
                "hookRefR2" => master.GetComponent<HERO>().hookRefR2,
                _ => null
            };
        }
    }

    [Guardian.Networking.RPC(Name = "netLaunch")]
    private void NetLaunch(Vector3 newPosition, PhotonMessageInfo info)
    {
        if (Guardian.AntiAbuse.Validators.HookValidator.IsLaunchValid(info))
        {
            nodes = new ArrayList() { newPosition };
        }
    }

    [Guardian.Networking.RPC(Name = "netUpdatePhase1")]
    private void NetUpdatePhase1(Vector3 newPosition, Vector3 masterPosition, PhotonMessageInfo info)
    {
        if (Guardian.AntiAbuse.Validators.HookValidator.IsPhaseUpdateValid(info))
        {
            lineRenderer.SetVertexCount(2);
            lineRenderer.SetPosition(0, newPosition);
            lineRenderer.SetPosition(1, masterPosition);
            base.transform.position = newPosition;
        }
    }

    [Guardian.Networking.RPC(Name = "netUpdateLeviSpiral")]
    private void NetUpdateLeviSpiral(Vector3 newPosition, Vector3 masterPosition, Vector3 masterrotation, PhotonMessageInfo info)
    {
        if (Guardian.AntiAbuse.Validators.HookValidator.IsLeviSpiralValid(info))
        {
            phase = 2;
            leviMode = true;
            GetSpiral(masterPosition, masterrotation);
            Vector3 b = masterPosition - (Vector3)spiralNodes[0];
            lineRenderer.SetVertexCount((int)((float)spiralNodes.Count - (float)spiralcount * 0.5f));
            for (int i = 0; (float)i <= (float)(spiralNodes.Count - 1) - (float)spiralcount * 0.5f; i++)
            {
                if (spiralcount < 5)
                {
                    Vector3 vector = (Vector3)spiralNodes[i] + b;
                    float num = (float)(spiralNodes.Count - 1) - (float)spiralcount * 0.5f;
                    vector = new Vector3(vector.x, vector.y * ((num - (float)i) / num) + newPosition.y * ((float)i / num), vector.z);
                    lineRenderer.SetPosition(i, vector);
                }
                else
                {
                    lineRenderer.SetPosition(i, (Vector3)spiralNodes[i] + b);
                }
            }
        }
    }

    public bool IsHooked()
    {
        return phase == 1;
    }

    private void GetSpiral(Vector3 masterposition, Vector3 masterrotation)
    {
        float num2 = 30f;
        float num4 = 0.5f;
        float num;
        float num3 = 0.05f + (float)spiralcount * 0.03f;
        if (spiralcount < 5)
        {
            Vector2 a = new Vector2(masterposition.x, masterposition.z);
            Vector3 position = base.gameObject.transform.position;
            float x = position.x;
            Vector3 position2 = base.gameObject.transform.position;
            float num5 = Vector2.Distance(a, new Vector2(x, position2.z));
            num = num5;
        }
        else
        {
            num = 1.2f + (float)(60 - spiralcount) * 0.1f;
        }
        num4 -= (float)spiralcount * 0.06f;
        float num6 = num / num2;
        float num7 = num3 / num2;
        float num8 = num7 * 2f * (float)Math.PI;
        num4 *= (float)Math.PI * 2f;
        spiralNodes = new ArrayList();
        for (int i = 1; (float)i <= num2; i++)
        {
            float num9 = (float)i * num6 * (1f + 0.05f * (float)i);
            float f = (float)i * num8 + num4 + (float)Math.PI * 2f / 5f + masterrotation.y * 0.0173f;
            float x2 = Mathf.Cos(f) * num9;
            float z = (0f - Mathf.Sin(f)) * num9;
            spiralNodes.Add(new Vector3(x2, 0f, z));
        }
    }

    private void SetLinePhase0()
    {
        if (master == null)
        {
            UnityEngine.Object.Destroy(rope);
            UnityEngine.Object.Destroy(base.gameObject);
        }
        else if (nodes.Count > 0)
        {
            Vector3 a = myRef.transform.position - (Vector3)nodes[0];
            lineRenderer.SetVertexCount(nodes.Count);
            for (int i = 0; i <= nodes.Count - 1; i++)
            {
                lineRenderer.SetPosition(i, (Vector3)nodes[i] + a * Mathf.Pow(0.75f, i));
            }
            if (nodes.Count > 1)
            {
                lineRenderer.SetPosition(1, myRef.transform.position);
            }
        }
    }

    [Guardian.Networking.RPC(Name = "setPhase")]
    private void SetPhase(int value)
    {
        phase = value;
    }

    [Guardian.Networking.RPC(Name = "setVelocityAndLeft")]
    private void SetVelocityAndLeft(Vector3 value, Vector3 v2, bool l)
    {
        velocity = value;
        velocity2 = v2;
        left = l;
        base.transform.rotation = Quaternion.LookRotation(value.normalized);
    }

    [Guardian.Networking.RPC(Name = "tieMeTo")]
    private void TieMeTo(Vector3 p)
    {
        base.transform.position = p;
    }

    // Deadly hooks
    private void HandleHookToObj(int viewId)
    {
        PhotonView pv = PhotonView.Find(viewId);
        if (pv == null || !Guardian.GuardianClient.Properties.DeadlyHooks.Value || !PhotonNetwork.isMasterClient) return;

        HERO hero = pv.gameObject.GetComponent<HERO>();
        if (hero == null || hero.HasDied()) return;

        string killer = GExtensions.AsString(base.photonView.owner.customProperties[PhotonPlayerProperty.Name]);
        if (killer.StripNGUI().Length < 1)
        {
            killer = "Player";
        }
        killer += $" [FFCC00]({base.photonView.owner.Id})[FFFFFF]";

        hero.MarkDead();
        hero.photonView.RPC("netDie", PhotonTargets.All, base.transform.position, false, -1, $"{killer}'s hook ", false);
    }

    [Guardian.Networking.RPC(Name = "tieMeToOBJ")]
    private void TieMeToObject(int id, PhotonMessageInfo info)
    {
        if (Guardian.AntiAbuse.Validators.HookValidator.IsHookTieValid(this, id, info))
        {
            base.transform.parent = PhotonView.Find(id).gameObject.transform;

            // Deadly hooks
            HandleHookToObj(id);
        }
    }

    public void Update1()
    {
        if (master == null)
        {
            RemoveMe();
        }
        else if (!isdestroying)
        {
            if (leviMode)
            {
                leviShootTime += Time.deltaTime;
                if (leviShootTime > 0.4f)
                {
                    phase = 2;
                    base.gameObject.GetComponent<MeshRenderer>().enabled = false;
                }
            }

            switch (phase)
            {
                case 0:
                    SetLinePhase0();
                    break;
                case 1:
                    Vector3 a = base.transform.position - myRef.transform.position;
                    Vector3 a2 = master.rigidbody.velocity;
                    float magnitude = a2.magnitude;
                    float magnitude2 = a.magnitude;
                    int value = (int)((magnitude2 + magnitude) / 5f);
                    value = Mathf.Clamp(value, 2, 6);
                    lineRenderer.SetVertexCount(value);
                    lineRenderer.SetPosition(0, myRef.transform.position);
                    int i = 1;
                    float num = Mathf.Pow(magnitude2, 0.3f);
                    for (; i < value; i++)
                    {
                        int num2 = value / 2;
                        float num3 = Mathf.Abs(i - num2);
                        float f = ((float)num2 - num3) / (float)num2;
                        f = Mathf.Pow(f, 0.5f);
                        float num4 = (num + magnitude) * 0.0015f * f;
                        lineRenderer.SetPosition(i, new Vector3(UnityEngine.Random.Range(0f - num4, num4), UnityEngine.Random.Range(0f - num4, num4), UnityEngine.Random.Range(0f - num4, num4)) + myRef.transform.position + a * ((float)i / (float)value) - Vector3.up * num * 0.05f * f - a2 * 0.001f * f * num);
                    }
                    lineRenderer.SetPosition(value - 1, base.transform.position);
                    break;
                case 2:
                    if (leviMode && !petraMode)
                    {
                        GetSpiral(master.transform.position, master.transform.rotation.eulerAngles);
                        Vector3 b = myRef.transform.position - (Vector3)spiralNodes[0];
                        lineRenderer.SetVertexCount((int)((float)spiralNodes.Count - (float)spiralcount * 0.5f));
                        for (int j = 0; (float)j <= (float)(spiralNodes.Count - 1) - (float)spiralcount * 0.5f; j++)
                        {
                            if (spiralcount < 5)
                            {
                                Vector3 position = (Vector3)spiralNodes[j] + b;
                                float num5 = (float)(spiralNodes.Count - 1) - (float)spiralcount * 0.5f;
                                float x = position.x;
                                float num6 = position.y * ((num5 - (float)j) / num5);
                                Vector3 position2 = base.gameObject.transform.position;
                                position = new Vector3(x, num6 + position2.y * ((float)j / num5), position.z);
                                lineRenderer.SetPosition(j, position);
                            }
                            else
                            {
                                lineRenderer.SetPosition(j, (Vector3)spiralNodes[j] + b);
                            }
                        }
                    }
                    else
                    {
                        lineRenderer.SetVertexCount(2);
                        lineRenderer.SetPosition(0, base.transform.position);
                        lineRenderer.SetPosition(1, myRef.transform.position);
                        killTime += Time.deltaTime * 0.2f;
                        lineRenderer.SetWidth(0.1f - killTime, 0.1f - killTime);
                        if (killTime > 0.1f)
                        {
                            RemoveMe();
                        }
                    }
                    break;
                case 3:
                    break;
                case 4:
                    base.gameObject.transform.position += velocity + velocity2 * Time.deltaTime;
                    ArrayList arrayList = nodes;
                    Vector3 position3 = base.gameObject.transform.position;
                    float x2 = position3.x;
                    Vector3 position4 = base.gameObject.transform.position;
                    float y = position4.y;
                    Vector3 position5 = base.gameObject.transform.position;
                    arrayList.Add(new Vector3(x2, y, position5.z));
                    Vector3 a3 = myRef.transform.position - (Vector3)nodes[0];
                    for (int k = 0; k <= nodes.Count - 1; k++)
                    {
                        lineRenderer.SetVertexCount(nodes.Count);
                        lineRenderer.SetPosition(k, (Vector3)nodes[k] + a3 * Mathf.Pow(0.5f, k));
                    }
                    killTime2 += Time.deltaTime;
                    if (killTime2 > 0.8f)
                    {
                        killTime += Time.deltaTime * 0.2f;
                        lineRenderer.SetWidth(0.1f - killTime, 0.1f - killTime);
                        if (killTime > 0.1f)
                        {
                            RemoveMe();
                        }
                    }
                    break;
            }

            // BEGIN Guardian
            if (lineRenderer.material != null)
            {
                float ropeLength = (base.transform.position - myRef.transform.position).magnitude;
                lineRenderer.material.mainTextureScale = new Vector2(tileScale * ropeLength, 1f);
            }
            // END: Guardian
        }
    }

    public void Disable()
    {
        phase = 2;
        killTime = 0f;
        if (IN_GAME_MAIN_CAMERA.Gametype == GameType.Multiplayer)
        {
            base.photonView.RPC("setPhase", PhotonTargets.Others, 2);
        }
    }

    public void RemoveMe()
    {
        // Anarchy
        Anarchy.Custom.Level.CustomAnarchyLevel.Instance.OnHookUntiedGround(this);

        isdestroying = true;
        if (IN_GAME_MAIN_CAMERA.Gametype == GameType.Singleplayer)
        {
            UnityEngine.Object.Destroy(rope);
            UnityEngine.Object.Destroy(base.gameObject);
        }
        else if (base.photonView.isMine)
        {
            PhotonNetwork.Destroy(base.photonView);
            PhotonNetwork.RemoveRPCs(base.photonView);
        }
    }

    [Guardian.Networking.RPC(Name = "killObject")]
    private void KillObject(PhotonMessageInfo info)
    {
        if (Guardian.AntiAbuse.Validators.HookValidator.IsKillObjectValid(info))
        {
            UnityEngine.Object.Destroy(rope);
            UnityEngine.Object.Destroy(base.gameObject);
        }
    }

    private void OnDestroy()
    {
        if (FengGameManagerMKII.Instance != null)
        {
            FengGameManagerMKII.Instance.RemoveHook(this);
        }
        if (myTitan != null)
        {
            myTitan.isHooked = false;
        }
        UnityEngine.Object.Destroy(rope);
    }

    private void FixedUpdate()
    {
        if ((phase == 2 || phase == 1) && leviMode)
        {
            spiralcount++;
            if (spiralcount >= 60)
            {
                isdestroying = true;
                RemoveMe();
                return;
            }
        }

        if (IN_GAME_MAIN_CAMERA.Gametype != GameType.Singleplayer && !base.photonView.isMine)
        {
            if (phase == 0)
            {
                base.gameObject.transform.position += velocity * Time.deltaTime * 50f + velocity2 * Time.deltaTime;
                nodes.Add(new Vector3(base.gameObject.transform.position.x, base.gameObject.transform.position.y, base.gameObject.transform.position.z));
            }
        }
        else if (phase == 0)
        {
            CheckTitan();
            base.gameObject.transform.position += velocity * Time.deltaTime * 50f + velocity2 * Time.deltaTime;
            LayerMask mask = 1 << LayerMask.NameToLayer("EnemyBox");
            LayerMask mask2 = 1 << LayerMask.NameToLayer("Ground");
            LayerMask mask3 = 1 << LayerMask.NameToLayer("NetworkObject");
            LayerMask layerMask = (int)mask | (int)mask2 | (int)mask3;
            bool flag = false;
            if ((nodes.Count <= 1) ? Physics.Linecast((Vector3)nodes[nodes.Count - 1], base.gameObject.transform.position, out RaycastHit hitInfo, layerMask.value) : Physics.Linecast((Vector3)nodes[nodes.Count - 2], base.gameObject.transform.position, out hitInfo, layerMask.value))
            {
                bool flag3 = true;
                if (hitInfo.collider.transform.gameObject.layer == LayerMask.NameToLayer("EnemyBox"))
                {
                    if (IN_GAME_MAIN_CAMERA.Gametype == GameType.Multiplayer)
                    {
                        base.photonView.RPC("tieMeToOBJ", PhotonTargets.Others, hitInfo.collider.transform.root.gameObject.GetPhotonView().viewID);
                    }
                    master.GetComponent<HERO>().lastHook = hitInfo.collider.transform.root;
                    base.transform.parent = hitInfo.collider.transform;
                }
                else if (hitInfo.collider.transform.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    master.GetComponent<HERO>().lastHook = null;

                    // Anarchy
                    Anarchy.Custom.Level.CustomAnarchyLevel.Instance.OnHookAttachedToGround(this, hitInfo.collider.gameObject);
                }
                else if (hitInfo.collider.transform.gameObject.layer == LayerMask.NameToLayer("NetworkObject") && hitInfo.collider.transform.gameObject.tag == "Player" && !leviMode)
                {
                    if (IN_GAME_MAIN_CAMERA.Gametype == GameType.Multiplayer)
                    {
                        int viewId = hitInfo.collider.transform.root.gameObject.GetPhotonView().viewID;
                        base.photonView.RPC("tieMeToOBJ", PhotonTargets.Others, viewId);

                        HandleHookToObj(viewId);
                    }
                    master.GetComponent<HERO>().HookToHuman(hitInfo.collider.transform.root.gameObject, base.transform.position);
                    base.transform.parent = hitInfo.collider.transform;
                    master.GetComponent<HERO>().lastHook = null;
                }
                else
                {
                    flag3 = false;
                }
                if (phase == 2)
                {
                    flag3 = false;
                }
                if (flag3)
                {
                    master.GetComponent<HERO>().Launch(hitInfo.point, left, leviMode);
                    base.transform.position = hitInfo.point;
                    if (phase != 2)
                    {
                        phase = 1;
                        if (IN_GAME_MAIN_CAMERA.Gametype == GameType.Multiplayer)
                        {
                            base.photonView.RPC("setPhase", PhotonTargets.Others, 1);
                            base.photonView.RPC("tieMeTo", PhotonTargets.Others, base.transform.position);
                        }
                        if (leviMode)
                        {
                            GetSpiral(master.transform.position, master.transform.rotation.eulerAngles);
                        }
                        flag = true;
                    }
                }
            }
            nodes.Add(new Vector3(base.gameObject.transform.position.x, base.gameObject.transform.position.y, base.gameObject.transform.position.z));
            if (flag)
            {
                return;
            }
            killTime2 += Time.deltaTime;
            if (killTime2 > 0.8f)
            {
                phase = 4;
                if (IN_GAME_MAIN_CAMERA.Gametype == GameType.Multiplayer)
                {
                    base.photonView.RPC("setPhase", PhotonTargets.Others, 4);
                }
            }
        }
    }

    public void CheckTitan()
    {
        GameObject main_object = Camera.main.GetComponent<IN_GAME_MAIN_CAMERA>().main_object;
        if (main_object == null || master == null || !(master == main_object) || !Physics.Raycast(layerMask: ((LayerMask)(1 << LayerMask.NameToLayer("PlayerAttackBox"))).value, origin: base.transform.position, direction: velocity, hitInfo: out RaycastHit hitInfo, distance: 10f))
        {
            return;
        }
        Collider collider = hitInfo.collider;
        if (!collider.name.Contains("PlayerDetectorRC"))
        {
            return;
        }
        TITAN component = collider.transform.root.gameObject.GetComponent<TITAN>();
        if (component != null)
        {
            if (myTitan == null)
            {
                myTitan = component;
                myTitan.isHooked = true;
            }
            else if (myTitan != component)
            {
                myTitan.isHooked = false;
                myTitan = component;
                myTitan.isHooked = true;
            }
        }
    }

    private void Start()
    {
        // Load custom textures and audio clips
        if (Guardian.Utilities.ResourceLoader.TryGetAsset("Custom/Textures/hook.png", out Texture2D hookTexture))
        {
            base.gameObject.renderer.material.mainTexture = hookTexture;
        }

        rope = (GameObject)UnityEngine.Object.Instantiate(Resources.Load("rope"));
        lineRenderer = rope.GetComponent<LineRenderer>();

        GameObject.Find("MultiplayerManager").GetComponent<FengGameManagerMKII>().AddHook(this);

        if (master == null) return;

        HERO parentHero = master.GetComponent<HERO>();
        if (parentHero == null) return;

        if (left)
        {
            if (parentHero._leftRopeMat != null)
            {
                lineRenderer.material = parentHero._leftRopeMat;
                tileScale = parentHero._leftRopeXScale;
            }
        }
        else
        {
            if (parentHero._rightRopeMat != null)
            {
                lineRenderer.material = parentHero._rightRopeMat;
                tileScale = parentHero._rightRopeXScale;
            }
        }
    }
}
