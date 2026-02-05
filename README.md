# NaturalSelectionSim
---
Simulating natural selection and speciation

# Table of Contents
1. Introduction
2. Overview
3. Some Case Studies

# Introduction

This simulation shows how natural selection physical and behaviour evolution.
I was particularly interested in creating an environment that woud drive sympatric speciation (where multiple species arise in the same geographical environemt).

# Overview
The simulation runs on a uniform environment populated by _creatures_.

## Creatures

A creature has a set size and speed. When a creature is a certain amount larger than another creature, a collision between the two creatures will result in the larger creature consuming the smaller creature. The size and speed of a character is balanced by how quickly it uses up energy.

### Representing Resources
In real life, organisms require energy, which they mainly get through photosynthesizing (autotrophs) or by consuming other organisms (heterotrophs). They also need a source of carbon. In this simulation, the only resource creatues use is "energy," which mimics both material and energy in real life.

Every organism passively generates a small amount of energy, representing photosynthensis. However, the amount of passive energy generation decreases as the population of creatures increase, reflecting how material resources in an environment is finite.

$ms^2+Km^.75$ represents the rate at which a creature uses energy, where m is the size, s is the speed, and k is a constant. $ms^2$ mirrors the equation of kinetic energy, the energy it takes for the creature to move. $Km^.75$ reflects _Kleiber's Law_. Kleiber's Law observes that the metabolic rate of a creature increases with its size, but bigger creatures use less energy per mass.

$$\Delta E = G_{autogen} (\frac {N_{max}-n}{N_{max}}) - (ms^2+Km^.75)$$

If a creature is energy positive if it have a positive $\Delta E$. If a creature is energy negative, it must eat other creatures to gain energy. By eating, a creature gains the energy of othe other creature, and additional energy proportional to the consumed creature's size.

### Behaviour

Creatures don't inherintally know to chase after smaller creatures or run from larger ones. Instead, their behaviour comes from a very simple neural network.

Each creature is aware of its size and speed, as well as the size and speed of other creatures inside its detect radius. The creature has a weight (between -1 and 1) for each of these values, and computes its response for each of its neighboring creatures. A negative number indicates an intent to move away from the neighbor, while a positive number indicates an intent to move towards it. This value is scaled inversly by the distance between the creatures. Intent for all neighboring creatures are summed up and then scaled to the creatues speed to determine its velocity.

### Reproduction

When a creature reaches a certain amount of energy `spawnThreshold` it will reproduce. Creatures in this simulation can only reproduce asexually. To do so a creature gives \~half of its energy to its offspring. All physical and behavioural traits of an offspring derives from its parent and is mutated by a certain amount.

#### Physical Traits
1. Speed
2. Size
3. Detect Range - a creature takes account of all other creatures inside its detect range when deciding how to move
4. Split Threshold - When a creatue's energy reaches its split threshold, it will reproduce
5. Spawn Distance - the offspring of a creatue spawns some distance away from the parent

<!--
<img src="Analysis/plot.png" alt="Static Plot" width=400px />
<img src="Analysis/plot_speed_size_sped" alt="Fast_Time_graph" width=400px />

<video width="320" height="240" controls>
  <source src="" type="data_1_18_620.mp4">
  Your browser does not support the video tag.
</video>

https://github.com/user-attachments/assets/3407ade9-2931-4192-b5f9-a9b38d8927f8

