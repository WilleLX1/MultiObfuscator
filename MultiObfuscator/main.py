check = [0x0a20affb, 0xf22cedc7, 0xb5116c48, 0xdc07085b,
         0x7f7ec707, 0x9212cb42, 0x7b14d7d5, 0x5a52ac02, 
         0xc167f227, 0x3d024536, 0x926ed2da, 0xa3caf083, 
         0x998405fa, 0xdd9527d2, 0x88c2812d, 0x874fd6f9, 
         0x966da564, 0xe465faf8, 0x9afc85db, 0xb1edf7f7, 
         0x0829107e, 0xe9064063, 0xe5635b39, 0x0c160479, 
         0x44196690, 0x7dec02f9, 0x7178f1c1, 0x5c2da069, 
         0x3488601c] 

ITERS = 1_000_000

a = 1337
n = 2**32
inv = pow(a, -1, n)

def scramble(inp):
    print('scrambling', inp)
    l = len(inp)

    for _ in range(ITERS):

        for i in range(0, l, 1):

            inp[i] = ((inp[i] * a) % n)
            inp[i] = inp[i] ^ inp[(i+1) % l]

    return inp


def unscramble(inp):
    print('unscrambling', inp)
    l = len(inp)

    for _ in range(ITERS):

        for i in range(0, l, 1):
            i = l-i-1

            # a = ( x * b ) % n
            # x = (a * inv) % n

            inp[i] = inp[i] ^ inp[(i+1) % l]
            inp[i] = ((inp[i] * inv) % n)

    return inp

u = (unscramble(check))
f = [chr(c) for c in u]
print(''.join(f))