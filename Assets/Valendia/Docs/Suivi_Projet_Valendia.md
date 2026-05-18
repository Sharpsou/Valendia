# Suivi Projet Valendia

Derniere mise a jour : 2026-05-17

## Vision

Valendia est un prototype Unity en premiere personne, jouable a la manette, centre sur l'exploration d'un grand paysage procedural inspire des images du dossier `img_DA`.

Direction artistique visee :

- paysage ouvert et lisible, avec grande vallee explorable ;
- rendu medium-poly / low-poly soigne, pas photorealiste ;
- reliefs montagneux clairs et silhouettes fortes en arriere-plan ;
- vegetation dense : herbes, touffes, arbres colores, rochers ;
- chemin naturel qui guide l'exploration sans transformer la scene en couloir.

## References Visuelles

Images sources :

- `img_DA/img1.jpg` : vallee lumineuse, chemin sinueux, rochers clairs, montagnes verticales, vegetation dense mais lisible.
- `img_DA/img2.jpg` : rendu low-poly colore, arbres stylises, prairie tres dense, palette chaude et vive.
- `img_DA/img3.jpg` : grandes montagnes facettees, arbres d'automne, plaine ouverte.

Synthese DA :

- garder la densite d'herbe et de vegetation ;
- privilegier les formes simples et facettees ;
- utiliser des masses colorees distinctes pour herbe, chemin, rochers, feuillages ;
- eviter le detail fin realiste au profit de silhouettes fortes.

## Etat Actuel

Skills installes via `skills.sh` dans `.agents/skills` :

- `unity-developer`
- `game-development`
- `procedural-generation`
- `bmad-help`
- `bmad-technical-research`
- `bmad-agent-architect`
- `bmad-create-architecture`
- `bmad-generate-project-context`
- `bmad-quick-dev`
- `bmad-code-review`

Fichiers ajoutes :

- `Assets/Valendia/Scripts/Runtime/ValendiaLandscapeGenerator.cs`
- `Assets/Valendia/Scripts/Runtime/ValendiaFirstPersonController.cs`
- `Assets/Valendia/Scripts/Editor/ValendiaPrototypeSceneBuilder.cs`
- `Assets/Valendia/ValendiaPrototype.unity`
- `Assets/Valendia/Docs/Valendia_Procedural_Landscape_Plan.md`
- `Assets/Valendia/Docs/Suivi_Projet_Valendia.md`

## Systeme Procedural

Le generateur actuel est volontairement simple, deterministe et modifiable dans l'Inspector.

Fonctions presentes :

- seed unique pour reproduire un monde ;
- generation de terrain en chunks ;
- mesh terrain medium-poly ;
- relief par bruit Perlin multi-octaves ;
- micro-relief de sol tres faible, masque sur le chemin et calme en bord de carte pour eviter les vallons parasites ;
- textures/normal maps procedurales legeres appliquees aux materiaux de sol pour casser l'effet trop lisse sans ajouter de GameObjects ;
- montagnes renforcees en peripherie ;
- chemin sinueux aplani ;
- sous-mesh distinct pour sol et chemin ;
- sous-meshs de terrain distincts pour prairie, bosquet d'automne, herbes dorees, champ lavande et scrub de montagne ;
- dispersion de rochers, arbres, touffes d'herbe, fleurs lavande et scrub ;
- arbres remplaces par des meshes facettes generes par code : troncs polygonaux, couronnes en lobes et variantes coniferes ;
- couronnes d'arbres rendues double-face, avec normales de blobs orientees vers l'exterieur et volumes de feuillage aplatis dessous pour eviter l'effet coque vide ou cone inverse vu par dessous ;
- herbe densifiee avec des touffes multi-brins double face, plus larges et plus nombreuses ;
- fleurs lavande regroupees en patchs plus visibles ;
- nappes de prairie/fleurs ajoutees sous forme de patches bas au sol pour eviter une densite seulement faite de petits traits ;
- vegetation speciale le long des bords du chemin pour renforcer la premiere lecture en camera joueur ;
- bosquets authores pres du chemin pour casser la repetition procedurale des arbres ;
- rubans floraux larges pour rapprocher les masses de vegetation de `img_DA/img2.jpg` ;
- ruban de chemin lisse separe du terrain pour eviter les gros triangles visibles au premier plan ;
- nuages stylises low-poly et skybox cyan ;
- bancs de nuages horizontaux plus illustratifs ;
- massifs calcaires lointains plus etages, moins coniques ;
- vegetation evitee sur le chemin ;
- placement limite des arbres selon pente et fertilite ;
- palette terrain/feuillage/chemin recalibree vers des verts, terres et calcaires moins pastel ;
- herbe, fleurs et feuillages configures pour recevoir/produire davantage d'ombres sans retour des anciens artefacts noirs.

Intention technique :

- iterer rapidement sur la composition ;
- choisir un seed satisfaisant ;
- fixer ensuite la scene en gardant le seed ou en bakant les objets generes.

## Controle Joueur

Prototype premiere personne :

- `CharacterController` Unity ;
- mouvement clavier/souris en fallback ;
- support manette via le nouveau Input System installe explicitement (`com.unity.inputsystem`) et projet regle en mode `Both` ;
- fallback legacy conserve pour les axes Input Manager si une manette n'est exposee que par l'ancien backend ;
- detection manette plus robuste : lecture de `Gamepad.current` ou du premier `Gamepad.all`, fallback `Joystick` pour les HID generiques, plus log des devices detectes au demarrage Play Mode ;
- stick gauche : deplacement ;
- stick droit : camera ;
- bouton sud : saut ;
- stick gauche clique ou bumper gauche : sprint.

## Decisions Techniques

- Commencer par un generateur editor-friendly avant d'optimiser fortement.
- Eviter les systemes complexes type DOTS/ECS tant que le volume exact de vegetation n'est pas valide.
- Garder la generation deterministe pour faciliter la validation artistique.
- Utiliser des chunks pour preparer une scene grande, meme si le premier prototype reste charge d'un bloc.
- Prevoir une phase de baking/fixation apres selection du seed.

## Scene Prototype

La scene jouable est maintenant creee automatiquement via `Valendia > Create Prototype Scene`.
`Assets/Valendia/ValendiaPrototype.unity` est un artefact genere local lourd et ignore par Git ; il doit etre regenere depuis Unity apres un clone.

Elle contient :

- un objet `Valendia World` avec le generateur ;
- un paysage genere et conserve dans la scene ;
- un `Valendia Player` avec `CharacterController` ;
- une `Main Camera` deja assignee au controleur ;
- une lumiere directionnelle chaude ;
- un brouillard leger pour donner de la profondeur aux montagnes ;
- des nuages proceduraux ;
- une ambiance plus naturelle, moins surexposee, avec ombres de vegetation plus lisibles ;
- un outil `Valendia > Create Prototype Preview` genere une image de controle locale dans `Assets/Valendia/Docs/ValendiaPrototypePreview.png`, egalement ignoree par Git.

Derniere passe visuelle :

- `grassTuftCount` porte a 360000 pour appliquer la densite forte du bord du chemin a toute la carte ;
- `pathEdgePatchCount` porte a 2400 pour densifier les bords du chemin en premiere personne ;
- correction de la passe ratee : suppression des accents de sol batches qui creaient des intersections de polygones ;
- suppression du micro-relief ajoute qui creait trop de vallons ;
- `heightScale` abaisse a 30 et `distantMountainStrength` a 0.12 pour revenir a un relief doux type `img_DA/img2.jpg` ;
- chemin rendu quasi non destructif dans la topographie pour eviter les parois trop proches ;
- `meadowPatchCount` stabilise a 2400, `flowerPatchCount` a 680 et `flowerRibbonCount` a 32 ;
- mini-forets authorees densifiees de 18 a 31 arbres par bosquet ;
- ajout de `forestPocketCount` a 12 pour creer des poches de foret hors chemin ;
- feuillages, herbe et fleurs en materiaux double-face ;
- canopees larges completees par un volume bas de feuillage visible quand le joueur passe dessous, puis remplacees par des meshes coussin moins pointus et legerement plus hauts ;
- palette recalee par cycles compares a `img_DA/img2.jpg` : ciel bleu-vert sombre, prairies vertes dominantes, accents olive/dore, feuillages automne et rares variations rose/violet portees par la vegetation ;
- lumiere directionnelle de fin d'apres-midi : soleil plus bas, chaud, avec ombres plus marquees et ambiante reduite ;
- repartition des biomes ajustee pour eviter le retour du grand tapis mauve ou jaune uniforme : le sol reste vert/olive neutre, les zones dorees et lavande passent par les brins d'herbe et fleurs ;
- suppression des anciens placages couleur `Painterly Meadow Patch` : remplaces par des lots spatiaux `Organic Meadow Grass Batches` composes de strokes/brins verticaux, plus organiques et plus compatibles performance ;
- variantes d'herbe vert frais, olive, dore et rose/lavande appliquees aux lots globaux, aux bords de chemin et aux bords de carte ;
- arbres ajustes vers `img_DA/img2.jpg` : moins de coniferes parasites, rochers plus bas, canopees moins en parasol avec moins de galette basse et plus de volume dans les lobes superieurs ;
- correction proportion arbres : feuillage broadleaf abaisse autour du sommet du tronc, tronc legerement raccourci, branches remontees pour entrer dans la masse de feuilles au lieu de rester visibles sous une canopee trop haute ;
- passe sol prudente : ajout d'un micro-relief de 12 cm max environ, attenue sur le chemin, plus texture organique et normal map 128x128 tuilees sur les materiaux de terrain sans toucher au ruban de chemin ;
- ajout d'un anneau visible de terrain de foothold avec `MeshCollider` pour prolonger le sol jouable jusqu'a la base des montagnes, sans barriere invisible ;
- validation batch du foothold : 8 patches de terrain supplementaires, `MeshCollider` terrain passes a 24, le joueur ne doit plus tomber entre le bord de map et les montagnes ;
- preview regeneree apres chaque passe couleur, derniere image de controle dans `Assets/Valendia/Docs/ValendiaPrototypePreview.png`.

Passe performance :

- le generateur expose deux profils : `HighVisual` pour la densite complete et `PlayableOptimized` par defaut pour une scene plus jouable ;
- l'herbe principale n'est plus generee en dizaines de milliers de GameObjects individuels ;
- les touffes d'herbe sont maintenant regroupees en lots spatiaux par palette (`Fresh/Olive/Golden/Rose Grass Batch`) pour reduire la charge CPU, les draw calls et la taille scene ;
- l'herbe des bords de chemin est aussi batchee (`Path Edge Grass Batch`) ;
- les lots d'herbe et strokes de prairie utilisent des `LODGroup` Unity avec meshes dense, moyen et leger ; le niveau suit donc la camera/joueur au runtime ;
- les brins d'herbe ne projettent plus d'ombres individuelles, mais recoivent toujours les ombres des arbres/relief ;
- les objets statiques decoratifs sont bakes par materiau et mode d'ombre apres generation ; les lots d'herbe restent separes pour laisser leurs `LODGroup` fonctionner ;
- les colliders de troncs, rochers, montagnes et terrain restent presents dans les deux profils pour permettre l'exploration libre sur toute la carte ;
- validation batch du LOD d'herbe : 309 `LODGroup`, avec 436 lots `Fresh Grass`, 180 `Olive Grass`, 124 `Golden Grass`, 28 `Rose Grass` et 468 lots `Meadow Stroke` ;
- validation batch du profil optimise avec colliders complets : environ 956 `MeshRenderer` incluant les niveaux LOD d'herbe, 12 lots bakes hors herbe, 1186 `BoxCollider` et 2135 `CapsuleCollider` dans la scene generee ;
- les branches vides de la hierarchie generee sont supprimees apres baking ;
- les ombres principales passent en hard shadows, avec distance d'ombre a 900 et 4 cascades pour conserver les ombres des nuages ;
- scene regeneree avec densite visuelle elevee, sans les anciens `Ground Brush` parasites ni placages de meadow roses.

La scene generee localement est disponible ici apres generation :

- `Assets/Valendia/ValendiaPrototype.unity`

## Hygiene Git

Le depot versionne les sources Unity, les references visuelles, les scripts, les packages et les reglages projet.
Les fichiers suivants restent locaux et regenerables :

- `Library/`, `Logs/`, `Temp/`, `UserSettings/` ;
- `.agents/` et `skills-lock.json` ;
- `Assets/Valendia/ValendiaPrototype.unity` ;
- `Assets/Valendia/Docs/ValendiaPrototypePreview.png`.

Cette decision evite de versionner une scene procedurale d'environ 460 Mo et garde Git centre sur les sources maintenables.

## Risques Connus

- La derniere passe DA assume une densite tres elevee pour valider l'intention visuelle ; l'herbe est batchee et geree par `LODGroup`, mais reste le principal budget GPU.
- Les meshes facettes maison ameliorent le rendu, mais une vraie phase GPU instancing/indirect rendering restera utile si l'herbe reste trop couteuse.
- Les materiaux generes en runtime ne sont pas encore des assets persistants.
- Les textures de sol sont generees en memoire par le generateur ; si elles conviennent visuellement, elles pourront etre bakees plus tard en assets partages.
- Les performances n'ont pas encore ete mesurees en Play Mode.
- La direction artistique progresse, mais reste trop procedurale/brute par rapport aux references : il faudra une passe asset/shape plus authoring pour les arbres, nuages et montagnes.

## Prochaines Etapes

1. Tester en Play Mode avec clavier/souris et manette.
2. Apres un clone ou un nettoyage local, regenerer la scene avec `Valendia > Create Prototype Scene`.
3. Faire une passe DA authoring :
   - formes d'arbres encore moins systematiques,
   - masses florales plus continues et moins ponctuelles,
   - transitions de biomes et zones dorees encore moins plates,
   - nuages avec contours moins polygonaux.
4. Ajuster le seed, les densites et les couleurs apres validation visuelle dans l'editeur et dans la preview.
5. Optimiser la vegetation avec GPU instancing ou indirect rendering si les `LODGroup` d'herbe restent trop couteux.
6. Ajouter des points d'interet visibles depuis le chemin.

## Definition Du Premier Jalonnement

Le premier jalonnement est atteint quand :

- Unity ouvre une scene Valendia jouable ;
- le joueur peut explorer en premiere personne a la manette ;
- le terrain est assez grand pour marcher plusieurs minutes ;
- le chemin, les montagnes, les rochers, l'herbe dense et les arbres stylises sont visibles ;
- le rendu evoque clairement les references `img_DA` ;
- le monde peut etre regenere par seed puis fixe.
