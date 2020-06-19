const condition = true

function saySomething(){
    console.log('just a log')
}

condition && saySomething()

let txt = 'javascript javascript';
let replacedTxt = txt.replace(/java/g, 'replaceNew');

console.log(replacedTxt);

const arr = [1,1,1,1,2,2,2,2,2,23,4,4,4,4,4,6,6,6,6,7,8,9]

let un_arr = [... new Set(arr)]
console.log(un_arr)