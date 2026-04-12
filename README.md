# Study
unity

see branches
- [photon experiment with `SimpleKCC`](https://github.com/Feacur/Study/tree/network/photon_kcc)  
  seems like host migration support in `SimpleKCC` is either quite poor or I miss something  
  for the purposes of learning a framework `SimpleKCC` is overcomplicated, use `NetworkTransform`
- [photon experiment bare](https://github.com/Feacur/Study/tree/network/photon_bare)  
  . entities, components, systems talk to each other via an event bus  
  . services are initialized on the app start and fetched via a locator  
  . avatars can run around and shoot arrows at each other  
  . arrows are dropped and can be picked up  
  . arrows are pooled for the both cases  
  . supports rejoining to session  
  . supports host migration  
