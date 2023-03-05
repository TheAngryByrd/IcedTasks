---
title: Tutorial 1
category: Tutorials
categoryindex: 1
index: 1
---

# Tutorial 1


Do this


    /// The Hello World of functional languages!
    let rec factorial x = 
      if x = 0 then 1 
      else x * (factorial (x - 1))

    let f10 = factorial 10
