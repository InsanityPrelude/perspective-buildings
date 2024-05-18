using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using static Perspective.ResourceBank;
using static Perspective.TextureBank;

namespace Perspective
{
	public class CompOffsetter : ThingComp
	{
		public Offsetter Props;
        public Vector3 currentOffset, cachedTrueCenter;
		public bool isOffset, isMirrored;
		Command_Action adjustGizmo, mirrorGizmo;

		public override void Initialize(CompProperties props)
		{
			base.Initialize(props);

			//Modextensions will be impersonating CompProperties, basically
			if (this.parent.def.HasModExtension<Offsetter>()) Props = this.parent.def.GetModExtension<Offsetter>();

			//Cache position
			cachedTrueCenter = this.parent.TrueCenter();

			adjustGizmo = new Command_Action()
				{
					defaultLabel = "Owl_Adjust".Translate(),
					defaultDesc = "Owl_AdjustDesc".Translate(),
					icon = iconAdjust,
					action = () => SetCurrentOffset()
				};
			mirrorGizmo = new Command_Action()
				{
					defaultLabel = "Owl_Mirror".Translate(),
					defaultDesc = "Owl_MirrorDesc".Translate(),
					icon = iconMirror,
					action = () => SetMirroredState()
				};
		}

		public override void PostDeSpawn(Map map)
		{
			offsetRegistry.Remove(this.parent.thingIDNumber);
			base.PostDeSpawn(map);
		}

		public override void PostExposeData()
		{
			if (Props != null)
			{
				Scribe_Values.Look<bool>(ref isMirrored, "mirrored", false, false);
				Scribe_Values.Look<Vector3>(ref currentOffset, "currentOffset", new Vector3(0,0,0), false);

				UpdateRegistry();
			}
		}

		public void UpdateRegistry()
		{
			isOffset = currentOffset != zero;
			cachedTrueCenter = this.parent.TrueCenter();

			if (this.parent.def.drawerType == DrawerType.MapMeshOnly || this.parent.def.drawerType == DrawerType.MapMeshAndRealTime)
			{
				this.parent.Map?.mapDrawer.MapMeshDirty(this.parent.Position, MapMeshFlagDefOf.Things);
			}

			if (isMirrored || currentOffset != zero)
			{
				if (!offsetRegistry.ContainsKey(this.parent.thingIDNumber)) offsetRegistry.Add(this.parent.thingIDNumber, this);
			}
			else offsetRegistry.Remove(this.parent.thingIDNumber);
		}

        public void SetCurrentOffset()
        {
			SoundDefOf.Click.PlayOneShotOnCamera(null);

			int index = Props.offsets.FindIndex(x => x == currentOffset);
			currentOffset = Props.offsets.Count - 1 == index ? zero : Props.offsets[index == -1 ? 0 : ++index];
			UpdateRegistry();
        }

		public void SetMirroredState()
        {
			SoundDefOf.Click.PlayOneShotOnCamera(null);
			isMirrored ^= true;
			UpdateRegistry();
        }

		public override IEnumerable<Gizmo> CompGetGizmosExtra()
		{
            if (Props != null && (this.parent?.Faction?.IsPlayer ?? false))
            {
                // Adjust
                yield return adjustGizmo;

                // Mirror
                if ((!this.parent.def.rotatable && Props.mirror != Offsetter.Override.False) || (this.parent.def.rotatable && Props.mirror == Offsetter.Override.True))
                {
                    yield return mirrorGizmo;
                }
            }
		}
	}
}
